import { useState, type FormEvent } from "react";
import { ROUTE_ROLES } from "../auth/roles";
import { ApiError } from "../api/client";
import { collectAppointments, collectInvoices } from "../api/exports";
import type { Invoice } from "../api/invoices";
import { buildCsv, downloadCsv, validateExportRange } from "../exports/csv";
import type { AppointmentExportRow } from "../api/exports";
import { Button, Card, EmptyState, ErrorState, LoadingState, PageHeader } from "../components/ui";
import { useAuthStore } from "../store/authStore";

const fieldClass =
  "field mt-1";

type PanelState = {
  status: "idle" | "loading" | "empty" | "error";
  message: string | null;
};

const idle: PanelState = { status: "idle", message: null };

function messageFrom(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

function appointmentCsv(rows: AppointmentExportRow[]): string {
  return buildCsv(
    ["Patient", "UHID", "Treatment", "Date", "Time", "Status"],
    rows.map((row) => [
      row.patientName,
      row.patientUhid,
      row.treatmentName,
      row.requestedDate,
      row.requestedTimeSlot,
      row.status
    ])
  );
}

function invoiceCsv(rows: Invoice[]): string {
  return buildCsv(
    ["Number", "Patient id", "Currency", "Status", "Total", "Paid", "Balance", "Raised", "Issued"],
    rows.map((row) => [
      row.invoiceNumber,
      row.patientId,
      row.currency,
      row.status,
      row.total.toFixed(2),
      row.amountPaid.toFixed(2),
      row.balance.toFixed(2),
      row.createdAt,
      row.issuedAt ?? ""
    ])
  );
}

export function ExportsPage() {
  const role = useAuthStore((state) => state.user?.role);
  const canExportInvoices = role !== undefined && ROUTE_ROLES.billing.includes(role);

  const [appointmentFrom, setAppointmentFrom] = useState("");
  const [appointmentTo, setAppointmentTo] = useState("");
  const [appointmentError, setAppointmentError] = useState<string | null>(null);
  const [appointmentPanel, setAppointmentPanel] = useState<PanelState>(idle);

  const [invoiceFrom, setInvoiceFrom] = useState("");
  const [invoiceTo, setInvoiceTo] = useState("");
  const [invoiceError, setInvoiceError] = useState<string | null>(null);
  const [invoicePanel, setInvoicePanel] = useState<PanelState>(idle);

  async function onAppointments(event: FormEvent) {
    event.preventDefault();
    const problem = validateExportRange(appointmentFrom, appointmentTo);
    if (problem) {
      setAppointmentError(problem);
      setAppointmentPanel(idle);
      return;
    }
    setAppointmentError(null);
    setAppointmentPanel({ status: "loading", message: null });
    try {
      const rows = await collectAppointments(appointmentFrom, appointmentTo);
      if (rows.length === 0) {
        setAppointmentPanel({ status: "empty", message: null });
        return;
      }
      downloadCsv(`appointments-${appointmentFrom}-to-${appointmentTo}.csv`, appointmentCsv(rows));
      setAppointmentPanel(idle);
    } catch (error: unknown) {
      setAppointmentPanel({
        status: "error",
        message: messageFrom(error, "Unable to export appointments.")
      });
    }
  }

  async function onInvoices(event: FormEvent) {
    event.preventDefault();
    const problem = validateExportRange(invoiceFrom, invoiceTo);
    if (problem) {
      setInvoiceError(problem);
      setInvoicePanel(idle);
      return;
    }
    setInvoiceError(null);
    setInvoicePanel({ status: "loading", message: null });
    try {
      const rows = await collectInvoices(invoiceFrom, invoiceTo);
      if (rows.length === 0) {
        setInvoicePanel({ status: "empty", message: null });
        return;
      }
      downloadCsv(`invoices-${invoiceFrom}-to-${invoiceTo}.csv`, invoiceCsv(rows));
      setInvoicePanel(idle);
    } catch (error: unknown) {
      setInvoicePanel({
        status: "error",
        message: messageFrom(error, "Unable to export invoices.")
      });
    }
  }

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Records"
        title="Exports"
        description="Download a spreadsheet of visits or bills for a date range of up to 31 days. Bills use the day the invoice was raised."
      />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <Card className="p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">Appointments</h2>
          <p className="mt-1 text-sm text-muted">Visits whose requested date falls in the range.</p>
          <form onSubmit={onAppointments} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Appointments from
              <input
                className={fieldClass}
                type="date"
                value={appointmentFrom}
                onChange={(event) => setAppointmentFrom(event.target.value)}
              />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Appointments to
              <input
                className={fieldClass}
                type="date"
                value={appointmentTo}
                onChange={(event) => setAppointmentTo(event.target.value)}
              />
            </label>
            {appointmentError ? (
              <p className="sm:col-span-2 text-sm text-status-error-fg" role="alert">
                {appointmentError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={appointmentPanel.status === "loading"}>
                Download appointments
              </Button>
            </div>
          </form>
          <div className="mt-4">
            {appointmentPanel.status === "loading" ? <LoadingState label="Preparing the appointments file…" /> : null}
            {appointmentPanel.status === "empty" ? <EmptyState title="No appointments in this range." /> : null}
            {appointmentPanel.status === "error" && appointmentPanel.message ? (
              <ErrorState message={appointmentPanel.message} onRetry={() => void onAppointments({ preventDefault() {} } as FormEvent)} />
            ) : null}
          </div>
        </Card>

        {canExportInvoices ? (
          <Card className="p-4 sm:p-6">
            <h2 className="font-display text-lg font-semibold text-ink">Invoices</h2>
            <p className="mt-1 text-sm text-muted">Bills raised between these dates, including drafts.</p>
            <form onSubmit={onInvoices} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
              <label className="block text-sm font-semibold text-ink">
                Invoices from
                <input
                  className={fieldClass}
                  type="date"
                  value={invoiceFrom}
                  onChange={(event) => setInvoiceFrom(event.target.value)}
                />
              </label>
              <label className="block text-sm font-semibold text-ink">
                Invoices to
                <input
                  className={fieldClass}
                  type="date"
                  value={invoiceTo}
                  onChange={(event) => setInvoiceTo(event.target.value)}
                />
              </label>
              {invoiceError ? (
                <p className="sm:col-span-2 text-sm text-status-error-fg" role="alert">
                  {invoiceError}
                </p>
              ) : null}
              <div className="sm:col-span-2">
                <Button type="submit" disabled={invoicePanel.status === "loading"}>
                  Download invoices
                </Button>
              </div>
            </form>
            <div className="mt-4">
              {invoicePanel.status === "loading" ? <LoadingState label="Preparing the invoices file…" /> : null}
              {invoicePanel.status === "empty" ? <EmptyState title="No invoices in this range." /> : null}
              {invoicePanel.status === "error" && invoicePanel.message ? (
                <ErrorState message={invoicePanel.message} onRetry={() => void onInvoices({ preventDefault() {} } as FormEvent)} />
              ) : null}
            </div>
          </Card>
        ) : null}
      </div>
    </div>
  );
}
