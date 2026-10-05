import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../api/client";
import { searchPatients, type Patient } from "../api/patients";
import {
  createPrescription,
  getPrescriptionHistory,
  issuePrescription,
  listPrescriptions,
  revisePrescription,
  type Prescription,
  type PrescriptionHistory,
  type PrescriptionItemInput,
  type PrescriptionStatus
} from "../api/prescriptions";
import { Badge, Button, Card, DataTable, PageHeader, type BadgeTone, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;
const MAX_ITEMS = 30;

const fieldClass =
  "field mt-1";

type ItemDraft = {
  name: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions: string;
};

const emptyItem = (): ItemDraft => ({
  name: "",
  dosage: "",
  frequency: "",
  duration: "",
  instructions: ""
});

function isGuid(value: string): boolean {
  const trimmed = value.trim().toLowerCase();
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(trimmed)
    && trimmed !== "00000000-0000-0000-0000-000000000000";
}

function messageFrom(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

function statusTone(status: PrescriptionStatus): BadgeTone {
  if (status === "Issued") return "success";
  if (status === "Cancelled") return "rejected";
  if (status === "Superseded") return "approved";
  return "pending";
}

export function validatePrescriptionDraft(patientId: string, appointmentId: string, items: ItemDraft[]): string | null {
  if (!patientId.trim()) return "Patient is required.";
  if (!isGuid(patientId)) return "Enter a valid patient id.";
  if (!appointmentId.trim()) return "Appointment is required.";
  if (!isGuid(appointmentId)) return "Enter a valid appointment id.";
  if (items.length === 0) return "Add at least one medicine.";
  if (items.length > MAX_ITEMS) return `A prescription can include at most ${MAX_ITEMS} medicines.`;

  for (let index = 0; index < items.length; index += 1) {
    const item = items[index];
    const label = `Medicine ${index + 1}`;
    const name = item.name.trim();
    const dosage = item.dosage.trim();
    const frequency = item.frequency.trim();
    const duration = item.duration.trim();
    const instructions = item.instructions.trim();
    if (!name) return `${label}: name is required.`;
    if (name.length > 160) return `${label}: name must be at most 160 characters.`;
    if (!dosage) return `${label}: dosage is required.`;
    if (dosage.length > 120) return `${label}: dosage must be at most 120 characters.`;
    if (!frequency) return `${label}: frequency is required.`;
    if (frequency.length > 120) return `${label}: frequency must be at most 120 characters.`;
    if (!duration) return `${label}: duration is required.`;
    if (duration.length > 80) return `${label}: duration must be at most 80 characters.`;
    if (instructions.length > 500) return `${label}: instructions must be at most 500 characters.`;
  }

  return null;
}

function toItems(items: ItemDraft[]): PrescriptionItemInput[] {
  return items.map((item) => ({
    name: item.name.trim(),
    dosage: item.dosage.trim(),
    frequency: item.frequency.trim(),
    duration: item.duration.trim(),
    instructions: item.instructions.trim() ? item.instructions.trim() : null
  }));
}

export function PrescriptionsPage() {
  const [patientFilter, setPatientFilter] = useState("");
  const [appliedPatientId, setAppliedPatientId] = useState("");
  const [filterError, setFilterError] = useState<string | null>(null);
  const [issuedOnly, setIssuedOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [rows, setRows] = useState<Prescription[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [patientId, setPatientId] = useState("");
  const [appointmentId, setAppointmentId] = useState("");
  const [patientQuery, setPatientQuery] = useState("");
  const [patientMatches, setPatientMatches] = useState<Patient[]>([]);
  const [items, setItems] = useState<ItemDraft[]>([emptyItem()]);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const [historyId, setHistoryId] = useState<string | null>(null);
  const [history, setHistory] = useState<PrescriptionHistory | null>(null);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [revisionItems, setRevisionItems] = useState<ItemDraft[]>([emptyItem()]);
  const [revisionError, setRevisionError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listPrescriptions({
      patientId: appliedPatientId || undefined,
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
        setLoadError(messageFrom(error, "Unable to load prescriptions."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [appliedPatientId, page, reloadKey]);

  const visibleRows = issuedOnly ? rows.filter((row) => row.status === "Issued") : rows;

  function upsert(prescription: Prescription) {
    const exists = rows.some((row) => row.id === prescription.id);
    setRows((current) =>
      current.some((row) => row.id === prescription.id)
        ? current.map((row) => (row.id === prescription.id ? prescription : row))
        : [prescription, ...current]
    );
    if (!exists) setTotalCount((count) => count + 1);
  }

  function updateItem(index: number, key: keyof ItemDraft, value: string, revision = false) {
    const setter = revision ? setRevisionItems : setItems;
    setter((current) => current.map((item, itemIndex) => (itemIndex === index ? { ...item, [key]: value } : item)));
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

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    const problem = validatePrescriptionDraft(patientId, appointmentId, items);
    setFormError(problem);
    if (problem) return;
    setSaving(true);
    try {
      const created = await createPrescription({
        patientId: patientId.trim(),
        appointmentId: appointmentId.trim(),
        items: toItems(items)
      });
      upsert(created);
      setFormOpen(false);
      setItems([emptyItem()]);
      setPatientId("");
      setAppointmentId("");
      setPatientMatches([]);
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to save the prescription."));
    } finally {
      setSaving(false);
    }
  }

  async function onIssue(prescription: Prescription) {
    setBusyId(prescription.id);
    setLoadError(null);
    try {
      upsert(await issuePrescription(prescription.id));
    } catch (error: unknown) {
      setLoadError(messageFrom(error, "Unable to issue the prescription."));
    } finally {
      setBusyId(null);
    }
  }

  async function openHistory(prescription: Prescription) {
    setHistoryId(prescription.id);
    setHistory(null);
    setHistoryError(null);
    setHistoryLoading(true);
    setReason("");
    setRevisionError(null);
    setRevisionItems(
      prescription.items.length > 0
        ? prescription.items.map((item) => ({
            name: item.name,
            dosage: item.dosage,
            frequency: item.frequency,
            duration: item.duration,
            instructions: item.instructions
          }))
        : [emptyItem()]
    );
    try {
      setHistory(await getPrescriptionHistory(prescription.id));
    } catch (error: unknown) {
      setHistoryError(messageFrom(error, "Unable to load the prescription history."));
    } finally {
      setHistoryLoading(false);
    }
  }

  async function onRevise(event: FormEvent) {
    event.preventDefault();
    if (!historyId) return;
    const itemProblem = validatePrescriptionDraft(
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
      revisionItems
    );
    const medicineProblem = itemProblem && !itemProblem.startsWith("Patient") && !itemProblem.startsWith("Appointment") && !itemProblem.startsWith("Enter a valid")
      ? itemProblem
      : null;
    if (!reason.trim()) {
      setRevisionError("A reason is required.");
      return;
    }
    if (reason.trim().length > 500) {
      setRevisionError("The reason must be at most 500 characters.");
      return;
    }
    if (medicineProblem) {
      setRevisionError(medicineProblem);
      return;
    }
    setSaving(true);
    setRevisionError(null);
    try {
      const revised = await revisePrescription(historyId, { reason: reason.trim(), items: toItems(revisionItems) });
      upsert(revised);
      setRows((current) => current.map((row) => (row.id === historyId ? { ...row, status: "Superseded" } : row)));
      setHistory(await getPrescriptionHistory(revised.id));
      setHistoryId(revised.id);
      setReason("");
    } catch (error: unknown) {
      setRevisionError(messageFrom(error, "Unable to revise the prescription."));
    } finally {
      setSaving(false);
    }
  }

  const columns: DataTableColumn<Prescription>[] = [
    {
      id: "chart",
      header: "Chart",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.doctorName}</p>
          <p className="text-xs text-muted">Revision {row.revisionNumber}</p>
        </div>
      )
    },
    {
      id: "visit",
      header: "Visit",
      render: (row) => <span className="font-mono text-xs">{row.appointmentId}</span>
    },
    {
      id: "status",
      header: "Status",
      render: (row) => <Badge tone={statusTone(row.status)}>{row.status}</Badge>
    },
    {
      id: "medicines",
      header: "Medicines",
      render: (row) => row.items.map((item) => item.name).join(", ") || "—"
    },
    {
      id: "actions",
      header: "Actions",
      render: (row) => (
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="secondary" onClick={() => void openHistory(row)}>
            {`View ${row.id}`}
          </Button>
          {row.status === "Draft" ? (
            <Button disabled={busyId === row.id} onClick={() => void onIssue(row)}>
              {busyId === row.id ? "Issuing…" : `Issue ${row.id}`}
            </Button>
          ) : null}
        </div>
      )
    }
  ];

  const issuedVersion = history?.versions.find((version) => version.id === historyId && version.status === "Issued");

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Clinical chart"
        title="Prescriptions"
        description="Write a herbal prescription for a patient's approved or completed visit, issue it, and keep later changes as revisions."
        action={
          <Button
            className="w-full !bg-hero-button !text-hero-deep hover:!bg-hero-button-hover sm:w-auto"
            onClick={() => {
              setFormOpen((open) => !open);
              setFormError(null);
            }}
          >
            {formOpen ? "Close form" : "New prescription"}
          </Button>
        }
      />

      {formOpen ? (
        <Card className="mb-6 p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">New prescription</h2>
          <form onSubmit={onCreate} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Patient
              <input className={fieldClass} value={patientId} onChange={(event) => setPatientId(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Appointment
              <input className={fieldClass} value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} />
            </label>
            <div className="flex flex-col gap-2 sm:col-span-2 sm:flex-row sm:items-end">
              <label className="block min-w-0 flex-1 text-sm font-semibold text-ink">
                Find patient
                <input
                  className={fieldClass}
                  placeholder="Name, phone, or UHID"
                  value={patientQuery}
                  onChange={(event) => setPatientQuery(event.target.value)}
                />
              </label>
              <Button type="button" variant="secondary" onClick={() => void findPatients()}>
                Find patient
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
            {items.map((item, index) => (
              <fieldset key={index} className="grid grid-cols-1 gap-3 rounded-xl border border-surface-border p-3 sm:col-span-2 sm:grid-cols-2">
                <legend className="px-1 text-sm font-semibold text-ink">{`Medicine ${index + 1}`}</legend>
                <label className="block text-sm font-semibold text-ink">
                  {`Medicine name ${index + 1}`}
                  <input className={fieldClass} value={item.name} onChange={(event) => updateItem(index, "name", event.target.value)} />
                </label>
                <label className="block text-sm font-semibold text-ink">
                  {`Dosage ${index + 1}`}
                  <input className={fieldClass} value={item.dosage} onChange={(event) => updateItem(index, "dosage", event.target.value)} />
                </label>
                <label className="block text-sm font-semibold text-ink">
                  {`Frequency ${index + 1}`}
                  <input className={fieldClass} value={item.frequency} onChange={(event) => updateItem(index, "frequency", event.target.value)} />
                </label>
                <label className="block text-sm font-semibold text-ink">
                  {`Duration ${index + 1}`}
                  <input className={fieldClass} value={item.duration} onChange={(event) => updateItem(index, "duration", event.target.value)} />
                </label>
                <label className="block text-sm font-semibold text-ink sm:col-span-2">
                  {`Instructions ${index + 1}`}
                  <input className={fieldClass} value={item.instructions} onChange={(event) => updateItem(index, "instructions", event.target.value)} />
                </label>
                {items.length > 1 ? (
                  <Button type="button" variant="secondary" onClick={() => setItems((current) => current.filter((_, itemIndex) => itemIndex !== index))}>
                    {`Remove medicine ${index + 1}`}
                  </Button>
                ) : null}
              </fieldset>
            ))}
            <div className="sm:col-span-2">
              <Button
                type="button"
                variant="secondary"
                disabled={items.length >= MAX_ITEMS}
                onClick={() => setItems((current) => [...current, emptyItem()])}
              >
                Add medicine
              </Button>
            </div>
            {formError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2" role="alert">
                {formError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Saving…" : "Save prescription"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      {historyId ? (
        <Card className="mb-6 p-4 sm:p-6">
          <div className="flex items-center justify-between gap-3">
            <h2 className="font-display text-lg font-semibold text-ink">Prescription history</h2>
            <Button variant="secondary" onClick={() => setHistoryId(null)}>
              Close history
            </Button>
          </div>
          {historyLoading ? <div className="mt-4"><p className="text-sm text-muted">Loading history…</p></div> : null}
          {historyError ? (
            <p className="mt-4 text-sm text-status-error-fg" role="alert">
              {historyError}
            </p>
          ) : null}
          {history ? (
            <div className="mt-4 space-y-4">
              {history.versions.map((version) => (
                <div key={version.id} className="rounded-xl border border-surface-border p-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge tone={statusTone(version.status)}>{version.status}</Badge>
                    <span className="text-sm text-muted">Revision {version.revisionNumber}</span>
                  </div>
                  <ul className="mt-2 space-y-1 text-sm text-ink">
                    {version.items.map((item) => (
                      <li key={item.id}>
                        {item.name} — {item.dosage}, {item.frequency}, {item.duration}
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
              {history.revisions.length === 0 ? (
                <p className="text-sm text-muted">No revisions recorded.</p>
              ) : (
                <ul className="space-y-2">
                  {history.revisions.map((revision) => (
                    <li key={revision.id} className="text-sm text-ink">
                      <span className="font-semibold">Revision {revision.revisionNumber}: </span>
                      {revision.reason}
                    </li>
                  ))}
                </ul>
              )}
              {issuedVersion ? (
                <form onSubmit={onRevise} className="grid grid-cols-1 gap-3" noValidate>
                  <h3 className="font-semibold text-ink">Revise issued prescription</h3>
                  <label className="block text-sm font-semibold text-ink">
                    Reason
                    <input className={fieldClass} value={reason} onChange={(event) => setReason(event.target.value)} />
                  </label>
                  {revisionItems.map((item, index) => (
                    <label key={index} className="block text-sm font-semibold text-ink">
                      {`Revised medicine ${index + 1}`}
                      <input className={fieldClass} value={item.name} onChange={(event) => updateItem(index, "name", event.target.value, true)} />
                    </label>
                  ))}
                  {revisionError ? (
                    <p className="text-sm text-status-error-fg" role="alert">
                      {revisionError}
                    </p>
                  ) : null}
                  <Button type="submit" disabled={saving}>
                    {saving ? "Saving…" : "Save revision"}
                  </Button>
                </form>
              ) : null}
            </div>
          ) : null}
        </Card>
      ) : null}

      <DataTable
        columns={columns}
        rows={visibleRows}
        getRowId={(row) => row.id}
        caption="Prescriptions"
        page={page}
        pageCount={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading prescriptions…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle={issuedOnly ? "No issued prescriptions on this page." : "No prescriptions found."}
        emptyDescription="Write a chart for an approved or completed visit."
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
              setPage(1);
            }}
          >
            <label className="block min-w-0 flex-1 text-sm font-semibold text-ink" htmlFor="prescription-patient">
              Patient id
              <input
                id="prescription-patient"
                className={fieldClass}
                value={patientFilter}
                onChange={(event) => setPatientFilter(event.target.value)}
              />
            </label>
            <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-ink">
              <input type="checkbox" checked={issuedOnly} onChange={(event) => setIssuedOnly(event.target.checked)} />
              Issued only
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
