import type { ReactNode } from "react";

type EmptyStateProps = {
  title: string;
  description?: string;
  action?: ReactNode;
  illustration?: ReactNode;
};

export function EmptyState({ title, description, action, illustration }: EmptyStateProps) {
  return (
    <div className="rounded-card border border-dashed border-field-border bg-surface-raised px-6 py-10 text-center" role="status">
      {illustration ? <div className="mb-4 flex justify-center">{illustration}</div> : null}
      <span className="mx-auto mb-3 flex h-11 w-11 items-center justify-center rounded-full bg-primary-muted text-primary" aria-hidden="true">
        <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M12 21c-4.5-2-7-5.6-7-10 0 0 3.5-.5 7 3 3.5-3.5 7-3 7-3 0 4.4-2.5 8-7 10Z" />
          <path d="M12 14V7" />
        </svg>
      </span>
      <p className="font-display text-lg font-semibold text-ink">{title}</p>
      {description ? <p className="mx-auto mt-2 max-w-md text-sm text-muted">{description}</p> : null}
      {action ? <div className="mt-4">{action}</div> : null}
    </div>
  );
}
