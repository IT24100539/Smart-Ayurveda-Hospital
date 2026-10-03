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
import { Badge, type BadgeTone } from "../../components/ui";
import {
  COMPLAINT_STATUSES,
  complaintStatusLabel,
  formatWhen,
  priorityLabel,
  COMPLAINT_PRIORITIES
} from "./labels";

const fieldClass =
  "field mt-1";
const secondaryButton =
  "rounded-lg border border-surface-border bg-surface-raised px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted disabled:opacity-60";

function statusTone(status: ComplaintStatus): BadgeTone {
  if (status === "Resolved") return "approved";
  if (status === "Escalated") return "error";
  if (status === "InProgress") return "pending";
  return "neutral";
}

function priorityTone(priority: ComplaintPriority): BadgeTone {
  return priority === "High" ? "error" : "neutral";
}

type QueueChip = "all" | ComplaintStatus | "overdue";

const CHIPS: { id: QueueChip; label: string }[] = [
  { id: "all", label: "All" },
  { id: "Open", label: "Open" },
  { id: "InProgress", label: "In progress" },
  { id: "Escalated", label: "Escalated" },
  { id: "Resolved", label: "Resolved" },
  { id: "overdue", label: "Overdue" }
];

export function ComplaintQueuePage() {
  const [chip, setChip] = useState<QueueChip>("all");
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

    listComplaints({ overdue: false, status: "", priority: "" })
      .then((complaints) => {
        if (requestId !== generation.current) {
          return;
        }
        const newestFirst = [...complaints].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
        setItems(newestFirst);
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
  }, [reloadKey]);

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

  const counts: Record<QueueChip, number> = {
    all: items.length,
    Open: items.filter((item) => item.status === "Open").length,
    InProgress: items.filter((item) => item.status === "InProgress").length,
    Escalated: items.filter((item) => item.status === "Escalated").length,
    Resolved: items.filter((item) => item.status === "Resolved").length,
    overdue: items.filter((item) => item.isOverdue).length
  };

  const visible = items.filter((item) => {
    if (priority && item.priority !== priority) {
      return false;
    }
    if (chip === "all") {
      return true;
    }
    if (chip === "overdue") {
      return item.isOverdue;
    }
    return item.status === chip;
  });

  return (
    <section aria-labelledby="complaint-queue-heading">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 id="complaint-queue-heading" className="font-serif text-lg font-semibold text-heading">
            Complaint queue
          </h2>
          <p className="mt-1 text-sm text-muted">
            Newest complaints are listed first. Overdue means an open complaint older than 5 days.
          </p>
        </div>
        <label className="block text-sm font-semibold" htmlFor="complaint-priority-filter">
          Priority
          <select
            id="complaint-priority-filter"
            className={fieldClass}
            value={priority}
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
      </div>
      <div className="mt-4 flex flex-wrap gap-2" role="group" aria-label="Complaint filters">
        {CHIPS.map((item) => (
          <button
            key={item.id}
            type="button"
            aria-pressed={chip === item.id}
            className={
              chip === item.id
                ? "rounded-full bg-primary px-3 py-1.5 text-sm font-semibold text-primary-on"
                : "rounded-full border border-surface-border bg-surface-raised px-3 py-1.5 text-sm font-medium text-ink hover:bg-primary-muted"
            }
            onClick={() => setChip(item.id)}
          >
            {item.label} ({counts[item.id]})
          </button>
        ))}
      </div>

      {phase === "loading" ? (
        <p className="mt-6 text-sm text-muted" role="status">
          Loading complaints…
        </p>
      ) : null}

      {phase === "error" && loadError ? (
        <div className="mt-6 rounded-xl border border-danger/30 bg-surface-raised p-4" role="alert">
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

      {phase === "ready" && visible.length === 0 ? (
        <p className="mt-6 rounded-xl border border-dashed border-surface-border bg-surface-raised p-6 text-sm text-muted" role="status">
          {chip === "overdue" ? "No overdue complaints." : "No complaints in the queue."}
        </p>
      ) : null}

      {phase === "ready" && visible.length > 0 ? (
        <div className="table-scroll mt-4 rounded-2xl border border-surface-border bg-surface-raised shadow-card" aria-label="Complaints">
          <table className="data-table min-w-[56rem]">
            <thead>
              <tr>
                <th>Complaint</th>
                <th>Priority</th>
                <th>Status</th>
                <th>Assigned to</th>
                <th>Update status</th>
              </tr>
            </thead>
            <tbody>
              {visible.map((complaint) => (
                <tr key={complaint.id} className={complaint.isOverdue ? "bg-status-pending-bg/60" : undefined}>
                  <td className="px-4 py-4 align-top">
                    <h3 className="font-semibold text-ink">{complaint.subject}</h3>
                    <p className="mt-1 text-sm text-muted">
                      {complaint.patientName} · {formatWhen(complaint.createdAt)}
                    </p>
                    <p className="mt-2 whitespace-pre-wrap text-sm text-ink">{complaint.description}</p>
                  </td>
                  <td className="px-4 py-4 align-top">
                    <div className="flex flex-col items-start gap-1">
                      <Badge tone={priorityTone(complaint.priority)}>{priorityLabel(complaint.priority)}</Badge>
                      {complaint.isOverdue ? <Badge tone="pending">Overdue</Badge> : null}
                    </div>
                  </td>
                  <td className="px-4 py-4 align-top">
                    <Badge tone={statusTone(complaint.status)}>{complaintStatusLabel(complaint.status)}</Badge>
                  </td>
                  <td className="px-4 py-4 align-top">
                    <label className="block max-w-xs text-sm font-semibold" htmlFor={`complaint-assignee-${complaint.id}`}>
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
                  </td>
                  <td className="px-4 py-4 align-top">
                    <label className="block max-w-xs text-sm font-semibold" htmlFor={`complaint-status-${complaint.id}`}>
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
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  );
}
