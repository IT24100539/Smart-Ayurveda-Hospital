import { useEffect, useState } from "react";
import { ApiError } from "../api/client";
import { listStaffNotifications, type StaffNotification } from "../api/notifications";
import { Badge, DataTable, PageHeader, type BadgeTone, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;

const typeLabels: Record<string, string> = {
  FeedbackReply: "Feedback reply",
  ComplaintUpdate: "Complaint update",
  ComplaintEscalated: "Complaint escalated",
  General: "General",
  FeedbackAlert: "Feedback alert",
  AppointmentApproved: "Visit approved",
  AppointmentRejected: "Visit declined",
  AppointmentRescheduled: "Visit rescheduled",
  AppointmentCancelled: "Visit cancelled",
  PrescriptionIssued: "Prescription issued",
  InvoiceIssued: "Invoice issued"
};

function typeTone(type: string): BadgeTone {
  if (type === "ComplaintEscalated" || type === "FeedbackAlert" || type === "AppointmentRejected") return "rejected";
  if (type === "AppointmentApproved" || type === "PrescriptionIssued" || type === "InvoiceIssued") return "success";
  if (type === "AppointmentCancelled") return "pending";
  return "approved";
}

function messageFrom(error: unknown): string {
  return error instanceof ApiError ? error.message : "Unable to load notifications.";
}

export function NotificationsPage() {
  const [reloadKey, setReloadKey] = useState(0);
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<StaffNotification[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listStaffNotifications()
      .then((items) => {
        if (cancelled) return;
        setRows(Array.isArray(items) ? items : []);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setRows([]);
        setLoadError(messageFrom(error));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const pageCount = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const visible = rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  const columns: DataTableColumn<StaffNotification>[] = [
    {
      id: "when",
      header: "When",
      render: (row) => <time dateTime={row.createdAt}>{new Date(row.createdAt).toLocaleString()}</time>
    },
    {
      id: "type",
      header: "Type",
      render: (row) => <Badge tone={typeTone(row.type)}>{typeLabels[row.type] ?? row.type}</Badge>
    },
    {
      id: "notice",
      header: "Notice",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.title}</p>
          <p className="max-w-xl text-sm text-muted">{row.message}</p>
        </div>
      )
    },
    {
      id: "read",
      header: "Status",
      render: (row) => <Badge tone={row.isRead ? "approved" : "pending"}>{row.isRead ? "Read" : "New"}</Badge>
    }
  ];

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Desk"
        title="Notifications"
        description="Alerts sent to your staff account: visit changes, issued prescriptions and bills, and feedback that needs a look."
      />
      <DataTable
        columns={columns}
        rows={visible}
        getRowId={(row) => row.id}
        caption="Staff notifications"
        page={page}
        pageCount={pageCount}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading notifications…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle="No notifications."
      />
    </div>
  );
}
