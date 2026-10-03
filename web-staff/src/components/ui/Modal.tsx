import { useId, type ReactNode } from "react";

type ModalProps = {
  title: string;
  description?: string;
  icon?: ReactNode;
  children: ReactNode;
  /** Tailwind max-width class for the panel. */
  widthClass?: string;
};

/** Visual shell for blocking dialogs: scrim, raised panel, gold-edged header. */
export function Modal({ title, description, icon, children, widthClass = "max-w-md" }: ModalProps) {
  const titleId = useId();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-scrim/60 p-4 backdrop-blur-sm">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={["max-h-full w-full overflow-y-auto rounded-modal border border-gold/40 bg-surface-raised p-6 text-ink shadow-card", widthClass].join(" ")}
      >
        <div className="flex items-center gap-3 border-b border-surface-border pb-4">
          {icon ? (
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gold-muted text-gold" aria-hidden="true">
              {icon}
            </div>
          ) : null}
          <div>
            <h2 id={titleId} className="font-display text-lg font-semibold text-heading">
              {title}
            </h2>
            {description ? <p className="text-xs text-muted">{description}</p> : null}
          </div>
        </div>
        {children}
      </div>
    </div>
  );
}
