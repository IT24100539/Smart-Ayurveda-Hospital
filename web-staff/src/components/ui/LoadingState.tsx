type LoadingStateProps = {
  label?: string;
};

export function LoadingState({ label = "Loading…" }: LoadingStateProps) {
  return (
    <div className="flex items-center justify-center gap-3 rounded-xl border border-surface-border bg-surface-raised px-6 py-10 text-sm text-muted" role="status">
      <span className="h-4 w-4 animate-spin rounded-full border-2 border-primary-muted border-t-primary" aria-hidden="true" />
      {label}
    </div>
  );
}
