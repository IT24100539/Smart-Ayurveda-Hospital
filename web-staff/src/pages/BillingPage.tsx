import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../api/client";
import {
  cancelInvoice,
  createInvoice,
  issueInvoice,
  listInvoices,
  listPatientVisits,
  recordInvoicePayment,
  type Invoice,
  type InvoiceLineInput,
  type InvoicePaymentMethod,
  type InvoiceStatus,
  type VisitOption
} from "../api/invoices";
import { searchPatients, type Patient } from "../api/patients";
import { Badge, Button, Card, DataTable, PageHeader, type BadgeTone, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;
const MAX_LINES = 30;
const MAX_DAYS = 366;
const MAX_DAILY_RATE = 1_000_000;
const METHODS: InvoicePaymentMethod[] = ["Cash", "Card", "BankTransfer"];

const fieldClass =
  "field mt-1";

type LineKind = "treatment" | "admission";

type LineDraft = {
  kind: LineKind;
  appointmentId: string;
  admissionId: string;
  days: string;
  dailyRate: string;
};

const emptyLine = (): LineDraft => ({
  kind: "treatment",
  appointmentId: "",
  admissionId: "",
  days: "1",
  dailyRate: ""
});

function isGuid(value: string): boolean {
  const trimmed = value.trim().toLowerCase();
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(trimmed)
    && trimmed !== "00000000-0000-0000-0000-000000000000";
}

function messageFrom(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

function todayIso(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${String(month)}-${day}`;
}

function middayIso(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, month - 1, day, 12, 0, 0, 0).toISOString();
}

function money(amount: number): string {
  return amount.toFixed(2);
}

function statusTone(status: InvoiceStatus): BadgeTone {
  if (status === "Paid") return "success";
  if (status === "Issued") return "approved";
  if (status === "Cancelled") return "rejected";
  return "pending";
}

function decimalProblem(value: string, label: string, max: number): string | null {
  const trimmed = value.trim();
  if (!trimmed) return `${label} is required.`;
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) return `${label} can have at most two decimal places.`;
  const amount = Number(trimmed);
  if (!(amount > 0)) return `${label} must be greater than zero.`;
  if (amount > max) return `${label} is too large.`;
  return null;
}

export function validateInvoiceDraft(patientId: string, currency: string, notes: string, lines: LineDraft[]): string | null {
  if (!patientId.trim()) return "Patient is required.";
  if (!isGuid(patientId)) return "Enter a valid patient id.";
  const code = currency.trim();
  if (code && !/^[A-Za-z]{3}$/.test(code)) return "Currency must be a three-letter code, or left blank for LKR.";
  if (notes.trim().length > 500) return "Notes must be at most 500 characters.";
  if (lines.length === 0) return "Add a treatment visit or an admission.";
  if (lines.length > MAX_LINES) return `An invoice can include at most ${MAX_LINES} lines.`;

  const seen = new Set<string>();
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index];
    const label = `Line ${index + 1}`;
    if (line.kind === "treatment") {
      if (!line.appointmentId.trim()) return `${label}: a treatment visit is required.`;
      if (!isGuid(line.appointmentId)) return `${label}: enter a valid appointment id.`;
      const key = `visit:${line.appointmentId.trim().toLowerCase()}`;
      if (seen.has(key)) return "Each treatment visit or admission can appear only once.";
      seen.add(key);
    } else {
      if (!line.admissionId.trim()) return `${label}: an admission is required.`;
      if (!isGuid(line.admissionId)) return `${label}: enter a valid admission id.`;
      if (!/^\d+$/.test(line.days.trim())) return `${label}: bill the admission in whole days.`;
      const days = Number(line.days);
      if (days < 1 || days > MAX_DAYS) return `${label}: an admission is billed in days, from 1 to ${MAX_DAYS}.`;
      const rate = decimalProblem(line.dailyRate, `${label}: daily rate`, MAX_DAILY_RATE);
      if (rate) return rate;
      const key = `stay:${line.admissionId.trim().toLowerCase()}`;
      if (seen.has(key)) return "Each treatment visit or admission can appear only once.";
      seen.add(key);
    }
  }

  return null;
}

export function validatePayment(amount: string, paidOn: string, reference: string): string | null {
  const amountProblem = decimalProblem(amount, "Payment amount", 9_999_999_999.99);
  if (amountProblem) return amountProblem;
  if (!paidOn) return "Payment date is required.";
  const year = Number(paidOn.slice(0, 4));
  if (!Number.isInteger(year) || year < 1900 || year > 2100) return "The payment date is outside the supported range.";
  if (paidOn > todayIso()) return "The payment date cannot be in the future.";
  if (reference.trim().length > 80) return "The reference must be at most 80 characters.";
  return null;
}

function toLines(lines: LineDraft[]): InvoiceLineInput[] {
  return lines.map((line) =>
    line.kind === "treatment"
      ? { appointmentId: line.appointmentId.trim(), admissionId: null, quantity: 1, unitPrice: null }
      : {
          appointmentId: null,
          admissionId: line.admissionId.trim(),
          quantity: Number(line.days),
          unitPrice: Number(line.dailyRate)
        }
  );
}

export function BillingPage() {
  const [patientFilter, setPatientFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [appliedPatientId, setAppliedPatientId] = useState("");
  const [appliedStatus, setAppliedStatus] = useState("");
  const [filterError, setFilterError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [rows, setRows] = useState<Invoice[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [patientId, setPatientId] = useState("");
  const [currency, setCurrency] = useState("");
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<LineDraft[]>([emptyLine()]);
  const [visits, setVisits] = useState<VisitOption[]>([]);
  const [patientQuery, setPatientQuery] = useState("");
  const [patientMatches, setPatientMatches] = useState<Patient[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [confirmId, setConfirmId] = useState<string | null>(null);

  const [paying, setPaying] = useState<Invoice | null>(null);
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState<InvoicePaymentMethod>("Cash");
  const [paidOn, setPaidOn] = useState(todayIso);
  const [reference, setReference] = useState("");
  const [paymentError, setPaymentError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listInvoices({
      patientId: appliedPatientId || undefined,
      status: appliedStatus || undefined,
      page,
      pageSize: PAGE_SIZE
    })
      .then((result) => {
        if (cancelled) return;
        setRows(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setRows([]);
        setTotalCount(0);
        setLoadError(messageFrom(error, "Unable to load invoices."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [appliedPatientId, appliedStatus, page, reloadKey]);

  function upsert(invoice: Invoice) {
    const exists = rows.some((row) => row.id === invoice.id);
    setRows((current) =>
      current.some((row) => row.id === invoice.id)
        ? current.map((row) => (row.id === invoice.id ? invoice : row))
        : [invoice, ...current]
    );
    if (!exists) setTotalCount((count) => count + 1);
  }

  function updateLine(index: number, patch: Partial<LineDraft>) {
    setLines((current) => current.map((line, lineIndex) => (lineIndex === index ? { ...line, ...patch } : line)));
  }

  async function findPatients() {
    setFormError(null);
    try {
      const result = await searchPatients(patientQuery);
      setPatientMatches(result.items);
      if (result.items.length === 0) setFormError("No patients matched that search.");
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to search patients."));
    }
  }

  async function loadVisits() {
    setFormError(null);
    if (!isGuid(patientId)) {
      setFormError("Enter a valid patient id before loading visits.");
      return;
    }
    try {
      const result = await listPatientVisits(patientId.trim());
      const billable = result.items.filter((visit) => visit.status === "Approved" || visit.status === "Completed");
      setVisits(billable);
      if (billable.length === 0) setFormError("No approved or completed treatment visits were found for this patient.");
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to load treatment visits."));
    }
  }

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    const problem = validateInvoiceDraft(patientId, currency, notes, lines);
    setFormError(problem);
    if (problem) return;
    setSaving(true);
    try {
      const created = await createInvoice({
        patientId: patientId.trim(),
        currency: currency.trim() ? currency.trim().toUpperCase() : null,
        notes: notes.trim() ? notes.trim() : null,
        lines: toLines(lines)
      });
      upsert(created);
      setFormOpen(false);
      setLines([emptyLine()]);
      setNotes("");
      setCurrency("");
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to save the invoice."));
    } finally {
      setSaving(false);
    }
  }

  async function onIssue(invoice: Invoice) {
    setBusyId(invoice.id);
    setLoadError(null);
    try {
      upsert(await issueInvoice(invoice.id));
    } catch (error: unknown) {
      setLoadError(messageFrom(error, "Unable to issue the invoice."));
    } finally {
      setBusyId(null);
    }
  }

  async function onCancel(invoice: Invoice) {
    setBusyId(invoice.id);
    setLoadError(null);
    try {
      upsert(await cancelInvoice(invoice.id));
      setConfirmId(null);
    } catch (error: unknown) {
      setLoadError(messageFrom(error, "Unable to cancel the invoice."));
    } finally {
      setBusyId(null);
    }
  }

  async function onPay(event: FormEvent) {
    event.preventDefault();
    if (!paying) return;
    const problem = validatePayment(amount, paidOn, reference);
    setPaymentError(problem);
    if (problem) return;
    setSaving(true);
    try {
      const updated = await recordInvoicePayment(paying.id, {
        amount: Number(amount),
        method,
        paidOn: middayIso(paidOn),
        reference: reference.trim() ? reference.trim() : null
      });
      upsert(updated);
      setPaying(null);
      setAmount("");
      setReference("");
    } catch (error: unknown) {
      setPaymentError(messageFrom(error, "Unable to record the payment."));
    } finally {
      setSaving(false);
    }
  }

  const columns: DataTableColumn<Invoice>[] = [
    { id: "number", header: "Invoice", render: (row) => <span className="font-semibold">{row.invoiceNumber}</span> },
    { id: "status", header: "Status", render: (row) => <Badge tone={statusTone(row.status)}>{row.status}</Badge> },
    { id: "total", header: "Total", render: (row) => `${row.currency} ${money(row.total)}` },
    { id: "balance", header: "Balance", render: (row) => `${row.currency} ${money(row.balance)}` },
    {
      id: "lines",
      header: "Lines",
      render: (row) => row.lines.map((line) => line.description).join(", ") || "—"
    },
    {
      id: "actions",
      header: "Actions",
      render: (row) => (
        <div className="flex flex-wrap justify-end gap-2">
          {row.status === "Draft" ? (
            <Button disabled={busyId === row.id} onClick={() => void onIssue(row)}>
              {busyId === row.id ? "Issuing…" : `Issue ${row.invoiceNumber}`}
            </Button>
          ) : null}
          {row.status === "Issued" ? (
            <Button
              variant="secondary"
              onClick={() => {
                setPaying(row);
                setAmount(money(row.balance));
                setPaidOn(todayIso());
                setPaymentError(null);
              }}
            >
              {`Record payment for ${row.invoiceNumber}`}
            </Button>
          ) : null}
          {row.status === "Draft" || (row.status === "Issued" && row.amountPaid === 0) ? (
            confirmId === row.id ? (
              <>
                <Button variant="danger" disabled={busyId === row.id} onClick={() => void onCancel(row)}>
                  {`Confirm cancel ${row.invoiceNumber}`}
                </Button>
                <Button variant="secondary" onClick={() => setConfirmId(null)}>
                  Keep invoice
                </Button>
              </>
            ) : (
              <Button variant="secondary" onClick={() => setConfirmId(row.id)}>
                {`Cancel ${row.invoiceNumber}`}
              </Button>
            )
          ) : null}
        </div>
      )
    }
  ];

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Front desk"
        title="Billing"
        description="Raise a bill from a panchakarma visit or an approved ward stay, issue it, and record cash, card, or transfer by hand."
        action={
          <Button
            className="w-full !bg-hero-button !text-hero-deep hover:!bg-hero-button-hover sm:w-auto"
            onClick={() => {
              setFormOpen((open) => !open);
              setFormError(null);
            }}
          >
            {formOpen ? "Close form" : "New invoice"}
          </Button>
        }
      />

      {formOpen ? (
        <Card className="mb-6 p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">New invoice</h2>
          <form onSubmit={onCreate} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Patient
              <input className={fieldClass} value={patientId} onChange={(event) => setPatientId(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Currency
              <input className={fieldClass} placeholder="LKR" value={currency} onChange={(event) => setCurrency(event.target.value)} />
            </label>
            <div className="flex flex-col gap-2 sm:col-span-2 sm:flex-row sm:items-end">
              <label className="block min-w-0 flex-1 text-sm font-semibold text-ink">
                Find patient
                <input className={fieldClass} placeholder="Name, phone, or UHID" value={patientQuery} onChange={(event) => setPatientQuery(event.target.value)} />
              </label>
              <Button type="button" variant="secondary" onClick={() => void findPatients()}>
                Find patient
              </Button>
              <Button type="button" variant="secondary" onClick={() => void loadVisits()}>
                Load visits
              </Button>
            </div>
            {patientMatches.length > 0 ? (
              <div className="flex flex-wrap gap-2 sm:col-span-2">
                {patientMatches.map((patient) => (
                  <Button key={patient.id} type="button" variant="secondary" onClick={() => setPatientId(patient.id)}>
                    {`${patient.firstName} ${patient.lastName}`}
                  </Button>
                ))}
              </div>
            ) : null}
            {lines.map((line, index) => (
              <fieldset key={index} className="grid grid-cols-1 gap-3 rounded-xl border border-surface-border p-3 sm:col-span-2 sm:grid-cols-2">
                <legend className="px-1 text-sm font-semibold text-ink">{`Line ${index + 1}`}</legend>
                <label className="block text-sm font-semibold text-ink">
                  {`Source ${index + 1}`}
                  <select
                    className={fieldClass}
                    value={line.kind}
                    onChange={(event) => updateLine(index, { kind: event.target.value as LineKind })}
                  >
                    <option value="treatment">Treatment visit</option>
                    <option value="admission">Admission</option>
                  </select>
                </label>
                {line.kind === "treatment" ? (
                  <label className="block text-sm font-semibold text-ink">
                    {`Appointment ${index + 1}`}
                    <input
                      className={fieldClass}
                      list={visits.length > 0 ? "billable-visits" : undefined}
                      value={line.appointmentId}
                      onChange={(event) => updateLine(index, { appointmentId: event.target.value })}
                    />
                  </label>
                ) : (
                  <>
                    <label className="block text-sm font-semibold text-ink">
                      {`Admission ${index + 1}`}
                      <input className={fieldClass} value={line.admissionId} onChange={(event) => updateLine(index, { admissionId: event.target.value })} />
                    </label>
                    <label className="block text-sm font-semibold text-ink">
                      {`Days ${index + 1}`}
                      <input className={fieldClass} inputMode="numeric" value={line.days} onChange={(event) => updateLine(index, { days: event.target.value })} />
                    </label>
                    <label className="block text-sm font-semibold text-ink">
                      {`Daily rate ${index + 1}`}
                      <input className={fieldClass} inputMode="decimal" value={line.dailyRate} onChange={(event) => updateLine(index, { dailyRate: event.target.value })} />
                    </label>
                  </>
                )}
              </fieldset>
            ))}
            <datalist id="billable-visits">
              {visits.map((visit) => (
                <option key={visit.id} value={visit.id}>
                  {`${visit.treatmentName} ${visit.requestedDate} ${visit.requestedTimeSlot}`}
                </option>
              ))}
            </datalist>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Notes
              <textarea className={`${fieldClass} min-h-24`} value={notes} onChange={(event) => setNotes(event.target.value)} />
            </label>
            <div className="sm:col-span-2">
              <Button type="button" variant="secondary" disabled={lines.length >= MAX_LINES} onClick={() => setLines((current) => [...current, emptyLine()])}>
                Add line
              </Button>
            </div>
            {formError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2" role="alert">
                {formError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : "Save invoice"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      {paying ? (
        <Card className="mb-6 p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">{`Record payment for ${paying.invoiceNumber}`}</h2>
          <p className="mt-1 text-sm text-muted">
            Balance due {paying.currency} {money(paying.balance)}. There is no payment gateway.
          </p>
          <form onSubmit={onPay} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Amount
              <input className={fieldClass} inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Method
              <select className={fieldClass} value={method} onChange={(event) => setMethod(event.target.value as InvoicePaymentMethod)}>
                {METHODS.map((item) => (
                  <option key={item} value={item}>
                    {item === "BankTransfer" ? "Bank transfer" : item}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink">
              Paid on
              <input className={fieldClass} type="date" value={paidOn} onChange={(event) => setPaidOn(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Reference
              <input className={fieldClass} value={reference} onChange={(event) => setReference(event.target.value)} />
            </label>
            {paymentError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2" role="alert">
                {paymentError}
              </p>
            ) : null}
            <div className="flex gap-2 sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : "Save payment"}
              </Button>
              <Button type="button" variant="secondary" onClick={() => setPaying(null)}>
                Close payment
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        caption="Invoices"
        page={page}
        pageCount={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading invoices…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle="No invoices found."
        emptyDescription="Create a bill from a treatment visit or an approved admission."
        filter={
          <form
            className="flex flex-col gap-3 sm:flex-row sm:items-end"
            onSubmit={(event) => {
              event.preventDefault();
              const trimmed = patientFilter.trim();
              if (trimmed && !isGuid(trimmed)) {
                setFilterError("Enter a valid patient id.");
                return;
              }
              setFilterError(null);
              setAppliedPatientId(trimmed);
              setAppliedStatus(statusFilter);
              setPage(1);
            }}
          >
            <label className="block min-w-0 flex-1 text-sm font-semibold text-ink" htmlFor="invoice-patient">
              Patient id
              <input id="invoice-patient" className={fieldClass} value={patientFilter} onChange={(event) => setPatientFilter(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="invoice-status">
              Status
              <select id="invoice-status" className={fieldClass} value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
                <option value="">Any status</option>
                <option value="Draft">Draft</option>
                <option value="Issued">Issued</option>
                <option value="Paid">Paid</option>
                <option value="Cancelled">Cancelled</option>
              </select>
            </label>
            <Button type="submit">Search</Button>
            {filterError ? (
              <p className="text-sm text-status-error-fg sm:basis-full" role="alert">
                {filterError}
              </p>
            ) : null}
          </form>
        }
      />
    </div>
  );
}
