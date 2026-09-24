import { useEffect, useRef, useState } from "react";
import {
  errorMessage,
  listAssignees,
  listComplaints,
  updateComplaintStatus,
  type ComplaintPriority,
  type ComplaintStatus,
  type ComplaintSummary,
  type StaffAssignee
} from "../../api/feedback";
import {
  COMPLAINT_STATUSES,
  complaintStatusLabel,
  formatWhen,
  priorityLabel,
  COMPLAINT_PRIORITIES
} from "./labels";

const fieldClass =
  "mt-1 w-full rounded-lg border border-surface-border bg-white px-3 py-2 text-sm outline-none ring-primary focus:ring-2";
const secondaryButton =
  "rounded-lg border border-surface-border bg-white px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted disabled:opacity-60";

function statusTone(status: ComplaintStatus): string {
  if (status === "Resolved") {
    return "bg-primary-muted text-primary-dark";
  }
  if (status === "Escalated") {
    return "bg-red-100 text-danger";
  }
  if (status === "InProgress") {
    return "bg-amber-100 text-amber-950";
  }
  return "bg-surface text-ink";
}

export function ComplaintQueuePage() {
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [status, setStatus] = useState<ComplaintStatus | "">("");
  const [priority, setPriority] = useState<ComplaintPriority | "">("");
  const [assignees, setAssignees] = useState<StaffAssignee[]>([]);
  const [reloadKey, setReloadKey] = useState(0);
  const [phase, setPhase] = useState<"loading" | "ready" | "error">("loading");
  const [loadError, setLoadError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [items, setItems] = useState<ComplaintSummary[]>([]);
  const [savingId, setSavingId] = useState<string | null>(null);
  const generation = useRef(0);

  useEffect(() => {
    const requestId = ++generation.current;
    setPhase("loading");
    setLoadError(null);
    setActionError(null);
    setItems([]);

    listComplaints({ overdue: overdueOnly, status, priority })
      .then((complaints) => {
        if (requestId !== generation.current) {
          return;
        }
        setItems(complaints);
        setPhase("ready");
      })
      .catch((err: unknown) => {
        if (requestId !== generation.current) {
          return;
        }
        setItems([]);
        setLoadError(errorMessage(err, "Unable to load complaints."));
        setPhase("error");
      });
  }, [overdueOnly, status, priority, reloadKey]);

  useEffect(() => {
    listAssignees()
      .then(setAssignees)
      .catch(() => setAssignees([]));
  }, []);

  async function changeStatus(complaint: ComplaintSummary, status: ComplaintStatus) {
    if (status === complaint.status) {
      return;
    }
    const requestId = generation.current;
    setSavingId(complaint.id);
    setActionError(null);
    setItems((current) => current.map((item) => (item.id === complaint.id ? { ...item, status } : item)));
    try {
      const updated = await updateComplaintStatus(complaint.id, status);
      if (requestId !== generation.current) {
        return;
      }
      setItems((current) => {
        if (overdueOnly && !updated.isOverdue) {
          return current.filter((item) => item.id !== updated.id);
        }
        return current.map((item) => (item.id === updated.id ? updated : item));
      });
    } catch (err: unknown) {
      if (requestId !== generation.current) {
        return;
      }
      setItems((current) => current.map((item) => (item.id === complaint.id ? complaint : item)));
      setActionError(errorMessage(err, "Unable to update the complaint."));
    } finally {
      setSavingId((current) => (current === complaint.id ? null : current));
    }
  }

  return (
    <section aria-labelledby="complaint-queue-heading">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 id="complaint-queue-heading" className="font-serif text-lg font-semibold text-primary-dark">
            Complaint queue
          </h2>
          <p className="mt-1 text-sm text-muted">
            Open complaints older than 5 days are overdue and ready to escalate.
          </p>
        </div>
        <div className="flex flex-wrap items-end gap-2">
          <label className="block text-sm font-semibold" htmlFor="complaint-status-filter">
            Status
            <select
              id="complaint-status-filter"
              className={fieldClass}
              value={status}
              disabled={overdueOnly}
              onChange={(event) => setStatus(event.target.value as ComplaintStatus | "")}
            >
              <option value="">Any status</option>
              {COMPLAINT_STATUSES.map((item) => (
                <option key={item} value={item}>
                  {complaintStatusLabel(item)}
                </option>
              ))}
            </select>
          </label>
          <label className="block text-sm font-semibold" htmlFor="complaint-priority-filter">
            Priority
            <select
              id="complaint-priority-filter"
              className={fieldClass}
              value={priority}
              disabled={overdueOnly}
              onChange={(event) => setPriority(event.target.value as ComplaintPriority | "")}
            >
              <option value="">Any priority</option>
              {COMPLAINT_PRIORITIES.map((item) => (
                <option key={item} value={item}>
                  {priorityLabel(item)}
                </option>
              ))}
            </select>
          </label>
          <button
            type="button"
            className={
              overdueOnly
                ? "rounded-lg bg-amber-700 px-3 py-1.5 text-sm font-semibold text-white"
                : secondaryButton
            }
            aria-pressed={overdueOnly}
            onClick={() => setOverdueOnly((current) => !current)}
          >
            Overdue
          </button>
        </div>
      </div>

      {phase === "loading" ? (
        <p className="mt-6 text-sm text-muted" role="status">
          Loading complaints…
        </p>
      ) : null}

      {phase === "error" && loadError ? (
        <div className="mt-6 rounded-xl border border-danger/30 bg-white p-4" role="alert">
          <p className="text-sm text-danger">{loadError}</p>
          <button type="button" className={`${secondaryButton} mt-3`} onClick={() => setReloadKey((key) => key + 1)}>
            Try again
          </button>
        </div>
      ) : null}

      {actionError ? (
        <p className="mt-4 text-sm text-danger" role="alert">
          {actionError}
        </p>
      ) : null}

      {phase === "ready" && items.length === 0 ? (
        <p className="mt-6 rounded-xl border border-dashed border-surface-border bg-surface-raised p-6 text-sm text-muted" role="status">
          {overdueOnly ? "No overdue complaints." : "No complaints in the queue."}
        </p>
      ) : null}

      {phase === "ready" && items.length > 0 ? (
        <ul className="mt-4 space-y-3" aria-label="Complaints">
          {items.map((complaint) => (
            <li
              key={complaint.id}
              className={
                complaint.isOverdue
                  ? "rounded-xl border border-amber-300 bg-amber-50 p-4"
                  : "rounded-xl border border-surface-border bg-surface-raised p-4"
              }
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h3 className="font-semibold text-ink">{complaint.subject}</h3>
                  <p className="mt-1 text-sm text-muted">
                    {complaint.patientName} · {formatWhen(complaint.createdAt)}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <span
                    className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
                      complaint.priority === "High" ? "bg-red-100 text-danger" : "bg-surface text-ink"
                    }`}
                  >
                    {priorityLabel(complaint.priority)}
                  </span>
                  <span
                    className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${statusTone(complaint.status)}`}
                  >
                    {complaintStatusLabel(complaint.status)}
                  </span>
                  {complaint.isOverdue ? (
                    <span className="inline-flex rounded-full bg-amber-200 px-2 py-0.5 text-xs font-semibold text-amber-950">
                      Overdue
                    </span>
                  ) : null}
                </div>
              </div>
              <p className="mt-3 whitespace-pre-wrap text-sm text-ink">{complaint.description}</p>
              <label className="mt-3 block max-w-xs text-sm font-semibold" htmlFor={`complaint-assignee-${complaint.id}`}>
                Assigned to
                <select
                  id={`complaint-assignee-${complaint.id}`}
                  className={fieldClass}
                  value={complaint.assignedTo ?? ""}
                  disabled={savingId === complaint.id}
                  onChange={(event) => {
                    const assigneeId = event.target.value;
                    if (!assigneeId || assigneeId === complaint.assignedTo) {
                      return;
                    }
                    setSavingId(complaint.id);
                    setActionError(null);
                    updateComplaintStatus(complaint.id, complaint.status, assigneeId)
                      .then((updated) => {
                        setItems((current) => current.map((item) => (item.id === updated.id ? updated : item)));
                      })
                      .catch((err: unknown) => {
                        setActionError(errorMessage(err, "Unable to assign the complaint."));
                      })
                      .finally(() => setSavingId((current) => (current === complaint.id ? null : current)));
                  }}
                >
                  <option value="">Unassigned</option>
                  {assignees.map((assignee) => (
                    <option key={assignee.id} value={assignee.id}>
                      {assignee.fullName}
                    </option>
                  ))}
                </select>
              </label>
              <label className="mt-3 block max-w-xs text-sm font-semibold" htmlFor={`complaint-status-${complaint.id}`}>
                Status
                <select
                  id={`complaint-status-${complaint.id}`}
                  className={fieldClass}
                  value={complaint.status}
                  disabled={savingId === complaint.id}
                  onChange={(event) => void changeStatus(complaint, event.target.value as ComplaintStatus)}
                >
                  {COMPLAINT_STATUSES.map((status) => (
                    <option key={status} value={status}>
                      {complaintStatusLabel(status)}
                    </option>
                  ))}
                </select>
              </label>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
