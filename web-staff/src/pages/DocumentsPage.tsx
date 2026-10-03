import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../api/client";
import {
  DOCUMENT_CATEGORIES,
  downloadMedicalDocument,
  listMedicalDocuments,
  uploadMedicalDocument,
  type DocumentCategory,
  type MedicalDocument
} from "../api/documents";
import { searchPatients, type Patient } from "../api/patients";
import { Badge, Button, Card, DataTable, PageHeader, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;
const TITLE_MAX = 160;
const SUMMARY_MAX = 2000;
const MAX_BYTES = 10 * 1024 * 1024;
const ALLOWED_TYPES = ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/gif"];

const fieldClass =
  "field mt-1";

const categoryLabels: Record<DocumentCategory, string> = {
  LabReport: "Lab report",
  PrescriptionScan: "Prescription scan",
  DiagnosticScan: "Diagnostic scan",
  DischargeSummary: "Discharge summary",
  TreatmentPlan: "Treatment plan",
  General: "General"
};

export type DocumentDraft = {
  patientId: string;
  title: string;
  category: DocumentCategory;
  summary: string;
  file: File | null;
};

export function validateDocumentDraft(draft: DocumentDraft): string | null {
  if (!draft.patientId.trim()) return "Patient is required.";
  if (!isGuid(draft.patientId)) return "Enter a valid patient id.";
  const title = draft.title.trim();
  if (!title) return "Title is required.";
  if (title.length > TITLE_MAX) return `Title must be at most ${TITLE_MAX} characters.`;
  if (draft.summary.trim().length > SUMMARY_MAX) return `Summary must be at most ${SUMMARY_MAX} characters.`;
  if (!DOCUMENT_CATEGORIES.includes(draft.category)) return "Choose a document category.";
  if (!draft.file) return "A document file is required.";
  const type = draft.file.type === "image/jpg" ? "image/jpeg" : draft.file.type;
  if (!ALLOWED_TYPES.includes(type)) {
    return "Document must be a PDF, JPEG, PNG, WebP, or GIF.";
  }
  if (draft.file.size > MAX_BYTES) {
    return "Document must be a PDF, JPEG, PNG, WebP, or GIF no larger than 10 MB.";
  }
  return null;
}

function isGuid(value: string): boolean {
  const trimmed = value.trim().toLowerCase();
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(trimmed)
    && trimmed !== "00000000-0000-0000-0000-000000000000";
}

function messageFrom(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function patientLabel(patient: Patient): string {
  return `${patient.firstName} ${patient.lastName} · ${patient.uhid}`;
}

export function DocumentsPage() {
  const [patientId, setPatientId] = useState("");
  const [appliedPatientId, setAppliedPatientId] = useState("");
  const [filterError, setFilterError] = useState<string | null>(null);
  const [patientQuery, setPatientQuery] = useState("");
  const [patientMatches, setPatientMatches] = useState<Patient[]>([]);
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [rows, setRows] = useState<MedicalDocument[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [category, setCategory] = useState<DocumentCategory>("General");
  const [summary, setSummary] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    if (!appliedPatientId) return;
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listMedicalDocuments({ patientId: appliedPatientId, page, pageSize: PAGE_SIZE })
      .then((result) => {
        if (cancelled) return;
        setRows(result.items);
        setTotalCount(result.totalCount);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setRows([]);
        setTotalCount(0);
        setLoadError(messageFrom(error, "Unable to load documents."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [appliedPatientId, page, reloadKey]);

  function onSearch(event: FormEvent) {
    event.preventDefault();
    const trimmed = patientId.trim();
    if (!trimmed) {
      setFilterError("Patient is required.");
      return;
    }
    if (!isGuid(trimmed)) {
      setFilterError("Enter a valid patient id.");
      return;
    }
    setFilterError(null);
    setPage(1);
    setAppliedPatientId(trimmed);
    setReloadKey((key) => key + 1);
  }

  async function findPatients() {
    setFilterError(null);
    try {
      const result = await searchPatients(patientQuery);
      setPatientMatches(result.items);
      if (result.items.length === 0) setFilterError("No matching patients.");
    } catch (error: unknown) {
      setFilterError(messageFrom(error, "Unable to search patients."));
    }
  }

  async function onUpload(event: FormEvent) {
    event.preventDefault();
    const draft: DocumentDraft = { patientId, title, category, summary, file };
    const problem = validateDocumentDraft(draft);
    if (problem) {
      setFormError(problem);
      return;
    }
    setFormError(null);
    setSaving(true);
    try {
      await uploadMedicalDocument({
        patientId: patientId.trim(),
        title: title.trim(),
        category,
        summary: summary.trim() ? summary.trim() : null,
        file: file as File
      });
      setTitle("");
      setSummary("");
      setFile(null);
      setCategory("General");
      setFormOpen(false);
      setAppliedPatientId(patientId.trim());
      setPage(1);
      setReloadKey((key) => key + 1);
    } catch (error: unknown) {
      setFormError(messageFrom(error, "Unable to upload the document."));
    } finally {
      setSaving(false);
    }
  }

  async function onDownload(row: MedicalDocument) {
    setDownloadError(null);
    setBusyId(row.id);
    try {
      const fileResult = await downloadMedicalDocument(row.id);
      const url = URL.createObjectURL(fileResult.blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = fileResult.fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error: unknown) {
      setDownloadError(messageFrom(error, "Unable to download the document."));
    } finally {
      setBusyId(null);
    }
  }

  const columns: DataTableColumn<MedicalDocument>[] = [
    {
      id: "title",
      header: "Title",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.title}</p>
          {row.summary ? <p className="max-w-sm truncate text-xs text-muted">{row.summary}</p> : null}
        </div>
      )
    },
    {
      id: "category",
      header: "Category",
      render: (row) => <Badge tone="approved">{categoryLabels[row.category] ?? row.category}</Badge>
    },
    {
      id: "size",
      header: "Size",
      render: (row) => formatBytes(row.fileSizeBytes)
    },
    {
      id: "uploaded",
      header: "Uploaded",
      render: (row) => <time dateTime={row.uploadedAt}>{new Date(row.uploadedAt).toLocaleString()}</time>
    },
    {
      id: "actions",
      header: "File",
      render: (row) => (
        <Button variant="secondary" disabled={busyId === row.id} onClick={() => void onDownload(row)}>
          {busyId === row.id ? "Downloading…" : `Download ${row.title}`}
        </Button>
      )
    }
  ];

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Clinical chart"
        title="Documents"
        description="Attach a lab report, prescription scan, or discharge summary to a patient's chart, then open it again from the list."
        action={
          <Button
            className="w-full !bg-hero-button !text-hero-deep hover:!bg-hero-button-hover sm:w-auto"
            onClick={() => {
              setFormOpen((open) => !open);
              setFormError(null);
            }}
          >
            {formOpen ? "Close form" : "Upload document"}
          </Button>
        }
      />

      {formOpen ? (
        <Card className="mb-6 p-4 sm:p-6">
          <h2 className="font-display text-lg font-semibold text-ink">Upload document</h2>
          <form onSubmit={onUpload} className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Title
              <input className={fieldClass} value={title} onChange={(event) => setTitle(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink">
              Category
              <select
                className={fieldClass}
                value={category}
                onChange={(event) => setCategory(event.target.value as DocumentCategory)}
              >
                {DOCUMENT_CATEGORIES.map((item) => (
                  <option key={item} value={item}>
                    {categoryLabels[item]}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              Summary
              <textarea className={fieldClass} rows={3} value={summary} onChange={(event) => setSummary(event.target.value)} />
            </label>
            <label className="block text-sm font-semibold text-ink sm:col-span-2">
              File
              <input
                className={fieldClass}
                type="file"
                accept="application/pdf,image/jpeg,image/png,image/webp,image/gif"
                onChange={(event) => setFile(event.target.files?.[0] ?? null)}
              />
            </label>
            {formError ? (
              <p className="sm:col-span-2 text-sm text-status-error-fg" role="alert">
                {formError}
              </p>
            ) : null}
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>
                {saving ? "Uploading…" : "Save document"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        caption="Medical documents"
        page={page}
        pageCount={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading documents…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle={appliedPatientId ? "No documents found." : "Choose a patient"}
        emptyDescription={appliedPatientId ? undefined : "Enter a patient id to list chart files."}
        filter={
          <form onSubmit={onSearch} className="grid grid-cols-1 gap-3 sm:grid-cols-2" noValidate>
            <label className="block text-sm font-semibold text-ink">
              Patient
              <input className={fieldClass} value={patientId} onChange={(event) => setPatientId(event.target.value)} />
            </label>
            <div className="flex items-end gap-2">
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
                    {patientLabel(patient)}
                  </Button>
                ))}
              </div>
            ) : null}
            {filterError ? (
              <p className="sm:col-span-2 text-sm text-status-error-fg" role="alert">
                {filterError}
              </p>
            ) : null}
            {downloadError ? (
              <p className="sm:col-span-2 text-sm text-status-error-fg" role="alert">
                {downloadError}
              </p>
            ) : null}
            <div>
              <Button type="submit">Search</Button>
            </div>
          </form>
        }
      />
    </div>
  );
}
