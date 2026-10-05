import { useEffect, useState } from "react";
import { ApiError, api } from "../api/client";
import { Badge, PageHeader, type BadgeTone } from "../components/ui";
import {
  decideWorkflow,
  getWorkflow,
  listWorkflows,
  type ApprovalStatus,
  type ToolResult,
  type ValidationResult,
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

const APPROVABLE_TYPES = new Set(["AdmissionRequest", "Feedback", "FeedbackReply"]);

const fieldClass =
  "field w-auto";
const secondaryButton =
  "rounded-lg border border-surface-border bg-surface-raised px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted disabled:opacity-60";

function errorMessage(error: unknown): string {
  return error instanceof ApiError ? error.message : "Something went wrong. Please try again.";
}

function agentLabel(name: string): string {
  return AGENTS.find((agent) => agent.value === name)?.label ?? name;
}

function statusLabel(status: ApprovalStatus): string {
  return STATUSES.find((item) => item.value === status)?.label ?? status;
}

function approvalTone(status: ApprovalStatus): BadgeTone {
  if (status === "Approved") return "approved";
  if (status === "Rejected") return "rejected";
  if (status === "NotRequired") return "neutral";
  return "pending";
}

function calledAt(result: ToolResult): string {
  return result.calledAt ?? result.called_at ?? "";
}

function formatWhen(value: string): string {
  if (!value) return "Time not recorded";
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return value;
  return parsed.toLocaleString();
}

function asStrings(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value
    .map((item) => {
      if (typeof item === "string") return item;
      if (item && typeof item === "object" && "step" in item && typeof (item as { step: unknown }).step === "string") {
        return (item as { step: string }).step;
      }
      return null;
    })
    .filter((item): item is string => Boolean(item));
}

function asToolResults(value: unknown): ToolResult[] {
  return Array.isArray(value) ? (value as ToolResult[]) : [];
}

function asValidationResults(value: unknown): ValidationResult[] {
  return Array.isArray(value) ? (value as ValidationResult[]) : [];
}

function asErrorList(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string" && item.trim().length > 0);
}

function canApprove(workflow: WorkflowExecution | null): boolean {
  if (!workflow || workflow.approvalStatus !== "Pending") return false;
  if (!workflow.relatedEntityId || !workflow.relatedEntityType) return false;
  return APPROVABLE_TYPES.has(workflow.relatedEntityType);
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
  const [filters, setFilters] = useState<WorkflowFilters>({ agentName: "", approvalStatus: "Pending" });
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
        setSelectedId((current) => {
          if (current && page.items.some((item) => item.id === current)) return current;
          return page.items[0]?.id ?? null;
        });
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
    if (decision !== "RevisionRequested" && !canApprove(selected)) {
      setError(
        selected.relatedEntityId
          ? "This agent run cannot be approved from here."
          : "This run has nothing for a vaidya to approve."
      );
      return;
    }
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
      setItems((current) => {
        if (filters.approvalStatus && filters.approvalStatus !== workflow.approvalStatus) {
          return current.filter((item) => item.id !== workflow.id);
        }
        return current.map((item) => (item.id === workflow.id ? workflow : item));
      });
      setOutcome(outcomeFrom(workflow, bedLabel));
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  const plan = asStrings(selected?.plan);
  const completed = new Set(asStrings(selected?.completedSteps));
  const toolResults = asToolResults(selected?.toolResults);
  const validationResults = asValidationResults(selected?.validationResults);
  const errors = asErrorList(selected?.errors);
  const approveEnabled = canApprove(selected);
  const revisionEnabled = selected?.approvalStatus === "Pending";

  return (
    <section className="mx-auto max-w-7xl space-y-4">
      <PageHeader
        kicker="Agent plans"
        title="AI approvals"
        description="Review pending agent plans before a vaidya confirms a bed or posts a reply draft."
        action={
          <button
            type="button"
            className="rounded-lg bg-hero-button px-3 py-2 text-sm font-semibold text-hero-deep hover:bg-hero-button-hover"
            onClick={() => setRefreshKey((value) => value + 1)}
          >
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

      {error ? (
        <p className="rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger" role="alert">
          {error}
        </p>
      ) : null}
      {outcome ? (
        <p
          className="rounded-lg border border-primary bg-primary-muted px-3 py-2 text-sm font-medium text-heading"
          role="status"
        >
          {outcome}
        </p>
      ) : null}

      <div className="grid gap-4 lg:grid-cols-[20rem_minmax(0,1fr)_minmax(0,1.1fr)]">
        <aside className="overflow-hidden rounded-2xl border border-surface-border bg-surface-raised" aria-label="Workflows">
          {loading ? <p className="p-3 text-sm text-muted">Loading workflows…</p> : null}
          {!loading && items.length === 0 ? (
            <p className="p-3 text-sm text-muted">
              {filters.approvalStatus === "Pending"
                ? "No pending plans. Ask for an AI reply draft or start a bed admission, then refresh."
                : "No workflows match these filters."}
            </p>
          ) : null}
          {items.length > 0 ? (
            <div className="table-scroll">
              <table className="data-table">
                <caption className="sr-only">Agent workflows</caption>
                <thead>
                  <tr>
                    <th>Agent</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.id} className={item.id === selectedId ? "bg-primary-muted/60" : undefined}>
                      <td className="px-3 py-2">
                        <button
                          type="button"
                          className="w-full text-left text-sm"
                          onClick={() => {
                            setOutcome(null);
                            setSelectedId(item.id);
                          }}
                        >
                          <span className="block font-medium text-ink">{agentLabel(item.agentName)}</span>
                          <span className="mt-1 block truncate text-muted">{item.objectiveText}</span>
                        </button>
                      </td>
                      <td className="px-3 py-2">
                        <Badge tone={approvalTone(item.approvalStatus)}>{statusLabel(item.approvalStatus)}</Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
        </aside>

        <section className="rounded-xl border border-surface-border bg-surface-raised p-4" aria-label="Plan">
          <h2 className="font-serif text-lg font-semibold text-heading">Plan</h2>
          {detailLoading ? <p className="mt-3 text-sm text-muted">Loading plan…</p> : null}
          {selected && !detailLoading ? (
            <>
              <p className="mt-3 text-sm text-ink">{selected.objectiveText}</p>
              {selected.relatedEntityType ? (
                <p className="mt-2 text-xs text-muted">
                  Related: {selected.relatedEntityType}
                  {selected.relatedEntityId ? ` · ${selected.relatedEntityId.slice(0, 8)}…` : ""}
                </p>
              ) : (
                <p className="mt-2 text-xs text-muted">No related hospital record for this run.</p>
              )}
              {plan.length === 0 ? (
                <p className="mt-3 text-sm text-muted">This workflow has no plan steps.</p>
              ) : (
                <ol className="mt-3 list-decimal space-y-2 pl-5 text-sm">
                  {plan.map((step, index) => (
                    <li key={`${index}-${step}`} className={completed.has(step) ? "text-heading" : "text-ink"}>
                      <span>{step}</span>
                      {completed.has(step) ? <span className="ml-2 text-xs text-primary">Done</span> : null}
                    </li>
                  ))}
                </ol>
              )}
            </>
          ) : null}
        </section>

        <section className="rounded-xl border border-surface-border bg-surface-raised p-4" aria-label="Execution">
          <div className="flex items-start justify-between gap-3">
            <h2 className="font-serif text-lg font-semibold text-heading">Execution</h2>
            {selected ? <Badge tone={approvalTone(selected.approvalStatus)}>{statusLabel(selected.approvalStatus)}</Badge> : null}
          </div>
          {selected ? (
            <>
              <h3 className="mt-4 text-sm font-semibold text-ink">Tool calls</h3>
              <ul className="mt-2 space-y-2">
                {toolResults.length === 0 ? <li className="text-sm text-muted">No tool calls yet.</li> : null}
                {toolResults.map((result, index) => (
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
                {validationResults.length === 0 ? <li className="text-sm text-muted">No validation results.</li> : null}
                {validationResults.map((result, index) => (
                  <li
                    key={`${result.check ?? "check"}-${index}`}
                    className={`rounded-full px-3 py-1 text-xs font-semibold ${
                      result.passed ? "bg-primary-muted text-heading" : "bg-danger/10 text-danger"
                    }`}
                  >
                    {result.passed ? "Pass" : "Fail"}
                    {result.check ? `: ${result.check}` : ""}
                  </li>
                ))}
              </ul>
              {errors.length > 0 ? (
                <>
                  <h3 className="mt-4 text-sm font-semibold text-ink">Errors</h3>
                  <ul className="mt-2 space-y-1 text-sm text-danger">
                    {errors.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </>
              ) : null}
              {!approveEnabled && selected.approvalStatus === "Pending" ? (
                <p className="mt-4 text-sm text-muted">
                  This pending run has no bed request or reply draft to approve. Open Feedback to request an AI draft, or
                  start a ward admission plan.
                </p>
              ) : null}
              {selected.approvalStatus === "NotRequired" ? (
                <p className="mt-4 text-sm text-muted">
                  No staff decision is required for this run. Treatment answers and information-only flows land here for the
                  log.
                </p>
              ) : null}
              <div className="mt-5 flex flex-wrap gap-2">
                <button
                  type="button"
                  className="rounded-lg bg-primary px-3 py-1.5 text-sm font-semibold text-primary-on disabled:opacity-60"
                  disabled={!approveEnabled || busy}
                  onClick={() => decide("Approve")}
                >
                  Approve
                </button>
                <button
                  type="button"
                  className="rounded-lg bg-danger px-3 py-1.5 text-sm font-semibold text-danger-on disabled:opacity-60"
                  disabled={!approveEnabled || busy}
                  onClick={() => decide("Reject")}
                >
                  Reject
                </button>
                <button
                  type="button"
                  className={secondaryButton}
                  disabled={!revisionEnabled || busy}
                  onClick={() => decide("RevisionRequested")}
                >
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
  return asToolResults(workflow.toolResults)
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
