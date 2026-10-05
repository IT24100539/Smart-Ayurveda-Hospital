import type { ReactNode } from "react";

export type ToastTone = "success" | "error" | "warning" | "info";

const accents: Record<ToastTone, string> = {
  success: "border-l-status-approved-fg text-status-approved-fg",
  error: "border-l-status-error-fg text-status-error-fg",
  warning: "border-l-status-pending-fg text-status-pending-fg",
  info: "border-l-status-success-fg text-status-success-fg"
};

type ToastProps = {
  tone?: ToastTone;
  title?: string;
  children: ReactNode;
  onDismiss?: () => void;
};

/** One toast. Colour is never the only cue: the tone is also spoken as the title. */
export function Toast({ tone = "info", title, children, onDismiss }: ToastProps) {
  return (
    <div
      role={tone === "error" ? "alert" : "status"}
      className={["flex items-start gap-3 rounded-xl border border-l-4 border-surface-border bg-surface-raised p-4 text-sm shadow-lg", accents[tone]].join(" ")}
    >
      <div className="min-w-0 flex-1">
        {title ? <p className="font-semibold">{title}</p> : null}
        <div className="text-ink">{children}</div>
      </div>
      {onDismiss ? (
        <button
          type="button"
          onClick={onDismiss}
          className="-m-1 inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-md text-muted hover:bg-primary-muted hover:text-ink"
        >
          <span className="sr-only">Dismiss</span>
          <svg viewBox="0 0 24 24" aria-hidden="true" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <path d="M6 6l12 12M18 6 6 18" />
          </svg>
        </button>
      ) : null}
    </div>
  );
}

/** Fixed stack in the bottom-right corner. Place <Toast> children inside. */
export function ToastRegion({ children }: { children?: ReactNode }) {
  return (
    <div aria-live="polite" className="pointer-events-none fixed inset-x-4 bottom-4 z-[60] flex flex-col items-end gap-2 sm:left-auto sm:w-96">
      <div className="pointer-events-auto flex w-full flex-col gap-2">{children}</div>
    </div>
  );
}
