import { useEffect, useState } from "react";
import { ApiError, api } from "../api/client";
import { PageHeader } from "../components/ui";
import {
  decideWorkflow,
  getWorkflow,
  listWorkflows,
  type ApprovalStatus,
  type WorkflowExecution,
  type WorkflowFilters
} from "../api/workflows";

const AGENTS = [
  { value: "", label: "All agents" },
  { value: "scheduling_bed", label: "Scheduling & bed" },
  { value: "treatment_info", label: "Treatment information" },
  { value: "feedback_support", label: "Feedback & support" },
  { value: "patient_info", label: "Patient information" }
] as const;

const STATUSES: { value: ApprovalStatus | ""; label: string }[] = [
  { value: "", label: "All statuses" },
  { value: "Pending", label: "Pending" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
  { value: "RevisionRequested", label: "Revision requested" },
  { value: "NotRequired", label: "Not required" }
];

const fieldClass =
  "rounded-lg border border-surface-border bg-white px-3 py-2 text-sm outline-none ring-primary focus:ring-2";
const secondaryButton =
  "rounded-lg border border-surface-border bg-white px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted disabled:opacity-60";

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : "Something went wrong. Please try again.";
}

function agentLabel(name: string): string {
  return AGENTS.find((agent) => agent.value === name)?.label ?? name;
}

function statusLabel(status: ApprovalStatus): string {
  return STATUSES.find((item) => item.value === status)?.label ?? status;
}

function calledAt(result: WorkflowExecution["toolResults"][number]): string {
  return result.calledAt ?? result.called_at ?? "";
}

function formatWhen(value: string): string {
  if (!value) return "Time not recorded";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return parsed.toLocaleString();
}

function asStrings(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === "string") : [];
}

type Bed = { id: string; bedLabel: string; isOccupied: boolean };
type Ward = { id: string; beds: Bed[] };

function outcomeFrom(workflow: WorkflowExecution, bedLabel: string | null): string | null {
  if (workflow.approvalStatus === "Approved" && workflow.relatedEntityType === "AdmissionRequest") {
    return bedLabel ? `Bed ${bedLabel} allocated` : "Bed allocated";
  }
  if (
    workflow.approvalStatus === "Approved" &&
    (workflow.relatedEntityType === "Feedback" || workflow.relatedEntityType === "FeedbackReply")
  ) {
    return "Reply posted";
  }
  if (workflow.approvalStatus === "Rejected") return "Request rejected";
  if (workflow.approvalStatus === "RevisionRequested") return "Revision requested";
  return null;
}

export function AiApprovalsPage() {
  const [filters, setFilters] = useState<WorkflowFilters>({ agentName: "", approvalStatus: "" });
  const [items, setItems] = useState<WorkflowExecution[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selected, setSelected] = useState<WorkflowExecution | null>(null);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    listWorkflows(filters)
      .then((page) => {
        if (!active) return;
        setItems(page.items);
        setSelectedId((current) => current ?? page.items[0]?.id ?? null);
      })
      .catch((caught) => {
        if (active) setError(errorMessage(caught));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [filters, refreshKey]);

  useEffect(() => {
    if (!selectedId) {
      setSelected(null);
      return;
    }
    let active = true;
    setDetailLoading(true);
    getWorkflow(selectedId)
      .then((workflow) => {
        if (active) setSelected(workflow);
      })
      .catch((caught) => {
        if (active) setError(errorMessage(caught));
      })
      .finally(() => {
        if (active) setDetailLoading(false);
      });
    return () => {
      active = false;
    };
  }, [selectedId, refreshKey]);

  async function decide(decision: "Approve" | "Reject" | "RevisionRequested") {
    if (!selected) return;
    setBusy(true);
    setError(null);
    setOutcome(null);
    const beforeWards =
      selected.relatedEntityType === "AdmissionRequest" ? await apiWards().catch(() => []) : [];
    try {
      await decideWorkflow(selected.id, decision);
      const workflow = await getWorkflow(selected.id);
      const wards =
        workflow.relatedEntityType === "AdmissionRequest" ? await apiWards().catch(() => []) : [];
      const bedLabel = allocatedBed(workflow, beforeWards, wards);
      setSelected(workflow);
      setItems((current) => current.map((item) => (item.id === workflow.id ? workflow : item)));
      setOutcome(outcomeFrom(workflow, bedLabel));
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  const plan = asStrings(selected?.plan);
  const completed = new Set(asStrings(selected?.completedSteps));
  const canDecide = selected?.approvalStatus === "Pending";

  return (
    <section className="mx-auto max-w-7xl space-y-4">
      <PageHeader
        kicker="Agent plans"
        title="AI approvals"
        description="Review agent plans before a vaidya confirms them."
        action={
          <button type="button" className="rounded-lg bg-white px-3 py-2 text-sm font-semibold text-primary-dark hover:bg-primary-muted" onClick={() => setRefreshKey((value) => value + 1)}>
            Refresh
          </button>
        }
      />
      <div className="flex flex-wrap gap-3">
        <label className="text-sm text-muted">
          Agent
          <select
            className={`${fieldClass} ml-2`}
            aria-label="Agent"
            value={filters.agentName}
            onChange={(event) => {
              setSelectedId(null);
              setOutcome(null);
              setFilters((current) => ({ ...current, agentName: event.target.value }));
            }}
          >
            {AGENTS.map((agent) => (
              <option key={agent.value || "all"} value={agent.value}>
                {agent.label}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm text-muted">
          Status
          <select
            className={`${fieldClass} ml-2`}
            aria-label="Approval status"
            value={filters.approvalStatus}
            onChange={(event) => {
              setSelectedId(null);
              setOutcome(null);
              setFilters((current) => ({
                ...current,
                approvalStatus: event.target.value as WorkflowFilters["approvalStatus"]
              }));
            }}
          >
            {STATUSES.map((status) => (
              <option key={status.value || "all"} value={status.value}>
                {status.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error ? <p className="text-sm text-danger">{error}</p> : null}
      {outcome ? (
        <p className="rounded-lg border border-primary bg-primary-muted px-3 py-2 text-sm font-medium text-primary-dark" role="status">
          {outcome}
        </p>
      ) : null}

      <div className="grid gap-4 lg:grid-cols-[16rem_minmax(0,1fr)_minmax(0,1.1fr)]">
        <aside className="rounded-xl border border-surface-border bg-surface-raised p-3" aria-label="Workflows">
          {loading ? <p className="text-sm text-muted">Loading workflows…</p> : null}
          {!loading && items.length === 0 ? <p className="text-sm text-muted">No workflows match these filters.</p> : null}
          <ul className="space-y-2">
            {items.map((item) => (
              <li key={item.id}>
                <button
                  type="button"
                  className={`w-full rounded-lg px-3 py-2 text-left text-sm ${
                    item.id === selectedId ? "bg-primary-muted text-primary-dark" : "hover:bg-surface"
                  }`}
                  onClick={() => {
                    setOutcome(null);
                    setSelectedId(item.id);
                  }}
                >
                  <span className="block font-medium">{agentLabel(item.agentName)}</span>
                  <span className="mt-1 block truncate text-muted">{item.objectiveText}</span>
                  <span className="mt-1 block text-xs">{statusLabel(item.approvalStatus)}</span>
                </button>
              </li>
            ))}
          </ul>
        </aside>

        <section className="rounded-xl border border-surface-border bg-surface-raised p-4" aria-label="Plan">
          <h2 className="font-serif text-lg font-semibold text-primary-dark">Plan</h2>
          {detailLoading ? <p className="mt-3 text-sm text-muted">Loading plan…</p> : null}
          {selected && !detailLoading ? (
            plan.length === 0 ? (
              <p className="mt-3 text-sm text-muted">This workflow has no plan steps.</p>
            ) : (
              <ol className="mt-3 list-decimal space-y-2 pl-5 text-sm">
                {plan.map((step, index) => (
                  <li key={`${index}-${step}`} className={completed.has(step) ? "text-primary-dark" : "text-ink"}>
                    <span>{step}</span>
                    {completed.has(step) ? <span className="ml-2 text-xs text-primary">Done</span> : null}
                  </li>
                ))}
              </ol>
            )
          ) : null}
        </section>

        <section className="rounded-xl border border-surface-border bg-surface-raised p-4" aria-label="Execution">
          <div className="flex items-start justify-between gap-3">
            <h2 className="font-serif text-lg font-semibold text-primary-dark">Execution</h2>
            {selected ? (
              <p className="rounded-full bg-primary-muted px-3 py-1 text-sm font-semibold text-primary-dark">
                {statusLabel(selected.approvalStatus)}
              </p>
            ) : null}
          </div>
          {selected ? (
            <>
              <h3 className="mt-4 text-sm font-semibold text-ink">Tool calls</h3>
              <ul className="mt-2 space-y-2">
                {selected.toolResults.length === 0 ? <li className="text-sm text-muted">No tool calls yet.</li> : null}
                {selected.toolResults.map((result, index) => (
                  <li key={`${result.tool ?? "tool"}-${index}`} className="rounded-lg border border-surface-border px-3 py-2 text-sm">
                    <p className="font-medium">{result.tool ?? "Tool"}</p>
                    <p className="text-xs text-muted">{formatWhen(calledAt(result))}</p>
                    <p className={result.succeeded === false ? "text-danger" : "text-primary"}>
                      {result.succeeded === false ? result.error || "Failed" : "Succeeded"}
                    </p>
                  </li>
                ))}
              </ul>
              <h3 className="mt-4 text-sm font-semibold text-ink">Validation</h3>
              <ul className="mt-2 flex flex-wrap gap-2">
                {selected.validationResults.length === 0 ? <li className="text-sm text-muted">No validation results.</li> : null}
                {selected.validationResults.map((result, index) => (
                  <li
                    key={`${result.check ?? "check"}-${index}`}
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      result.passed ? "bg-primary-muted text-primary-dark" : "bg-danger/10 text-danger"
                    }`}
                  >
                    {result.passed ? "Pass" : "Fail"}
                    {result.check ? `: ${result.check}` : ""}
                  </li>
                ))}
              </ul>
              <div className="mt-5 flex flex-wrap gap-2">
                <button type="button" className="rounded-lg bg-primary px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-60" disabled={!canDecide || busy} onClick={() => decide("Approve")}>
                  Approve
                </button>
                <button type="button" className="rounded-lg bg-danger px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-60" disabled={!canDecide || busy} onClick={() => decide("Reject")}>
                  Reject
                </button>
                <button type="button" className={secondaryButton} disabled={!canDecide || busy} onClick={() => decide("RevisionRequested")}>
                  Request revision
                </button>
              </div>
            </>
          ) : (
            <p className="mt-3 text-sm text-muted">Select a workflow to review its log.</p>
          )}
        </section>
      </div>
    </section>
  );
}

async function apiWards(): Promise<Ward[]> {
  return api.request<Ward[]>("/wards");
}

function wardIdOf(workflow: WorkflowExecution): string | undefined {
  return workflow.toolResults
    .map((result) => result.output?.ward_id ?? result.output?.wardId)
    .find((value): value is string => typeof value === "string");
}

function wardFor(wards: Ward[], wardId: string | undefined): Ward | undefined {
  return wards.find((item) => item.id === wardId) ?? wards[0];
}

function allocatedBed(workflow: WorkflowExecution, before: Ward[], after: Ward[]): string | null {
  if (workflow.relatedEntityType !== "AdmissionRequest" || workflow.approvalStatus !== "Approved") return null;
  const wardId = wardIdOf(workflow);
  const previous = new Set(
    (wardFor(before, wardId)?.beds ?? []).filter((bed) => bed.isOccupied).map((bed) => bed.id)
  );
  const occupied = (wardFor(after, wardId)?.beds ?? []).filter((bed) => bed.isOccupied);
  const added = occupied.filter((bed) => !previous.has(bed.id));
  if (added.length === 1) return added[0].bedLabel;
  return occupied.length === 1 ? occupied[0].bedLabel : null;
}
