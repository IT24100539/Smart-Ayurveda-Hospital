import type { ReactNode } from "react";
import { Button } from "./Button";
import { EmptyState } from "./EmptyState";
import { ErrorState } from "./ErrorState";
import { LoadingState } from "./LoadingState";

export type DataTableColumn<T> = {
  id: string;
  header: string;
  sortable?: boolean;
  render: (row: T) => ReactNode;
};

export type DataTableSort = {
  columnId: string;
  direction: "asc" | "desc";
};

type DataTableProps<T> = {
  columns: DataTableColumn<T>[];
  rows: T[];
  getRowId: (row: T) => string;
  caption: string;
  filter?: ReactNode;
  sort?: DataTableSort | null;
  onSort?: (columnId: string) => void;
  page: number;
  pageCount: number;
  onPageChange: (page: number) => void;
  loading?: boolean;
  loadingLabel?: string;
  error?: string | null;
  onRetry?: () => void;
  emptyTitle?: string;
  emptyDescription?: string;
};

export function DataTable<T>({
  columns,
  rows,
  getRowId,
  caption,
  filter,
  sort,
  onSort,
  page,
  pageCount,
  onPageChange,
  loading = false,
  loadingLabel,
  error,
  onRetry,
  emptyTitle = "Nothing to show",
  emptyDescription
}: DataTableProps<T>) {
  return (
    <div className="overflow-hidden rounded-xl border border-surface-border bg-surface-raised shadow-sm">
      {filter ? <div className="border-b border-surface-border px-4 py-3">{filter}</div> : null}
      {loading ? (
        <div className="p-4">
          <LoadingState label={loadingLabel} />
        </div>
      ) : error ? (
        <div className="p-4">
          <ErrorState message={error} onRetry={onRetry} />
        </div>
      ) : rows.length === 0 ? (
        <div className="p-4">
          <EmptyState title={emptyTitle} description={emptyDescription} />
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <caption className="sr-only">{caption}</caption>
            <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-muted">
              <tr>
                {columns.map((column) => {
                  const active = sort?.columnId === column.id;
                  const direction = active ? sort.direction : "none";
                  return (
                    <th key={column.id} scope="col" className="px-4 py-3" aria-sort={direction === "asc" ? "ascending" : direction === "desc" ? "descending" : "none"}>
                      {column.sortable && onSort ? (
                        <button type="button" className="inline-flex items-center gap-1 text-muted hover:text-ink" onClick={() => onSort(column.id)}>
                          {column.header}
                          <span aria-hidden="true">{active ? (sort.direction === "asc" ? "↑" : "↓") : "↕"}</span>
                        </button>
                      ) : (
                        column.header
                      )}
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-border">
              {rows.map((row) => (
                <tr key={getRowId(row)}>
                  {columns.map((column) => (
                    <td key={column.id} className="px-4 py-3 text-ink">
                      {column.render(row)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <div className="flex items-center justify-between border-t border-surface-border px-4 py-3">
        <p className="text-sm text-muted">
          Page {page} of {Math.max(pageCount, 1)}
        </p>
        <div className="flex gap-2">
          <Button variant="secondary" disabled={page <= 1 || loading} onClick={() => onPageChange(page - 1)}>
            Previous
          </Button>
          <Button variant="secondary" disabled={page >= pageCount || loading} onClick={() => onPageChange(page + 1)}>
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}
