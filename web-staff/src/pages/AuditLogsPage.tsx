import { useEffect, useState, type FormEvent } from "react";
import { listAuditLogs, type AuditLog } from "../api/auditLogs";
import { ApiError } from "../api/client";
import { Badge, Button, DataTable, PageHeader, type BadgeTone, type DataTableColumn } from "../components/ui";

const PAGE_SIZE = 20;
const QUERY_MAX = 200;
const FIELD_MAX = 64;

const fieldClass =
  "field mt-1";

const actions = ["View", "Create", "Update", "Delete"] as const;
const entities = ["Patient", "Appointment", "ClinicalRecord", "Invoice"] as const;

type Filters = {
  query: string;
  action: string;
  entityName: string;
  entityId: string;
  fromDate: string;
  toDate: string;
};

const emptyFilters = (): Filters => ({
  query: "",
  action: "",
  entityName: "",
  entityId: "",
  fromDate: "",
  toDate: ""
});

function yearOutOfRange(value: string): boolean {
  if (!value) return false;
  const year = Number(value.slice(0, 4));
  return !Number.isInteger(year) || year < 1900 || year > 2100;
}

export function validateAuditFilters(filters: Filters): string | null {
  if (filters.query.trim().length > QUERY_MAX) {
    return `Search must be at most ${QUERY_MAX} characters.`;
  }
  if (filters.entityId.trim().length > FIELD_MAX) {
    return `Record id must be at most ${FIELD_MAX} characters.`;
  }
  if (yearOutOfRange(filters.fromDate) || yearOutOfRange(filters.toDate)) {
    return "Dates must be between 1900 and 2100.";
  }
  if (filters.fromDate && filters.toDate && filters.toDate < filters.fromDate) {
    return "The end date must be on or after the start date.";
  }
  return null;
}

function startOfLocalDay(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, month - 1, day, 0, 0, 0, 0).toISOString();
}

function endOfLocalDay(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, month - 1, day, 23, 59, 59, 999).toISOString();
}

function actionTone(action: string): BadgeTone {
  if (action === "Create") return "success";
  if (action === "Update") return "pending";
  if (action === "Delete") return "rejected";
  return "approved";
}

function messageFrom(error: unknown): string {
  return error instanceof ApiError ? error.message : "Unable to load the audit log.";
}

export function AuditLogsPage() {
  const [draft, setDraft] = useState<Filters>(emptyFilters);
  const [applied, setApplied] = useState<Filters>(emptyFilters);
  const [filterError, setFilterError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [rows, setRows] = useState<AuditLog[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(null);
    listAuditLogs({
      query: applied.query,
      action: applied.action || undefined,
      entityName: applied.entityName || undefined,
      entityId: applied.entityId,
      fromDate: applied.fromDate ? startOfLocalDay(applied.fromDate) : undefined,
      toDate: applied.toDate ? endOfLocalDay(applied.toDate) : undefined,
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
        setLoadError(messageFrom(error));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [applied, page, reloadKey]);

  function update<K extends keyof Filters>(key: K, value: Filters[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
    setFilterError(null);
  }

  function onApply(event: FormEvent) {
    event.preventDefault();
    const problem = validateAuditFilters(draft);
    if (problem) {
      setFilterError(problem);
      return;
    }
    setFilterError(null);
    setPage(1);
    setApplied({
      query: draft.query.trim(),
      action: draft.action,
      entityName: draft.entityName,
      entityId: draft.entityId.trim(),
      fromDate: draft.fromDate,
      toDate: draft.toDate
    });
  }

  function onClear() {
    const cleared = emptyFilters();
    setDraft(cleared);
    setApplied(cleared);
    setFilterError(null);
    setPage(1);
  }

  const columns: DataTableColumn<AuditLog>[] = [
    {
      id: "when",
      header: "When",
      render: (row) => <time dateTime={row.createdAt}>{new Date(row.createdAt).toLocaleString()}</time>
    },
    {
      id: "actor",
      header: "Actor",
      render: (row) => (
        <div>
          <p className="font-semibold">{row.actorEmail || "—"}</p>
          <p className="text-xs text-muted">{row.actorRole || "—"}</p>
        </div>
      )
    },
    {
      id: "action",
      header: "Action",
      render: (row) => <Badge tone={actionTone(row.action)}>{row.action || "—"}</Badge>
    },
    {
      id: "record",
      header: "Record",
      render: (row) => (
        <div>
          <p>{row.entityName || "—"}</p>
          <p className="max-w-[12rem] truncate text-xs text-muted" title={row.entityId}>
            {row.entityId || "—"}
          </p>
        </div>
      )
    },
    { id: "target", header: "Target", render: (row) => row.targetEmail || "—" },
    {
      id: "details",
      header: "Details",
      render: (row) => (
        <span className="block max-w-xs truncate" title={row.details}>
          {row.details || "—"}
        </span>
      )
    },
    { id: "ip", header: "IP address", render: (row) => row.ipAddress || "—" }
  ];

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Administration"
        title="Audit log"
        description="See who viewed or changed a patient record, appointment, clinical chart, or invoice."
      />
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        caption="Audit log"
        page={page}
        pageCount={Math.max(1, Math.ceil(totalCount / PAGE_SIZE))}
        onPageChange={setPage}
        loading={loading}
        loadingLabel="Loading audit log…"
        error={loadError}
        onRetry={() => setReloadKey((key) => key + 1)}
        emptyTitle="No audit records found."
        emptyDescription="Try a wider date range or clear the filters."
        filter={
          <form onSubmit={onApply} className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3" noValidate>
            <label className="block text-sm font-semibold text-ink sm:col-span-2 lg:col-span-3" htmlFor="audit-search">
              Search
              <input
                id="audit-search"
                className={fieldClass}
                placeholder="Actor email, record id, or details"
                value={draft.query}
                onChange={(event) => update("query", event.target.value)}
              />
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="audit-action">
              Action
              <select id="audit-action" className={fieldClass} value={draft.action} onChange={(event) => update("action", event.target.value)}>
                <option value="">Any action</option>
                {actions.map((action) => (
                  <option key={action} value={action}>
                    {action}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="audit-entity">
              Record type
              <select
                id="audit-entity"
                className={fieldClass}
                value={draft.entityName}
                onChange={(event) => update("entityName", event.target.value)}
              >
                <option value="">Any record</option>
                {entities.map((entity) => (
                  <option key={entity} value={entity}>
                    {entity}
                  </option>
                ))}
              </select>
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="audit-entity-id">
              Record id
              <input
                id="audit-entity-id"
                className={fieldClass}
                value={draft.entityId}
                onChange={(event) => update("entityId", event.target.value)}
              />
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="audit-from">
              From
              <input
                id="audit-from"
                className={fieldClass}
                type="date"
                value={draft.fromDate}
                onChange={(event) => update("fromDate", event.target.value)}
              />
            </label>
            <label className="block text-sm font-semibold text-ink" htmlFor="audit-to">
              To
              <input
                id="audit-to"
                className={fieldClass}
                type="date"
                value={draft.toDate}
                onChange={(event) => update("toDate", event.target.value)}
              />
            </label>
            <div className="flex flex-wrap items-end gap-2">
              <Button type="submit">Apply filters</Button>
              <Button type="button" variant="secondary" onClick={onClear}>
                Clear filters
              </Button>
            </div>
            {filterError ? (
              <p className="text-sm text-status-error-fg sm:col-span-2 lg:col-span-3" role="alert">
                {filterError}
              </p>
            ) : null}
          </form>
        }
      />
    </div>
  );
}
