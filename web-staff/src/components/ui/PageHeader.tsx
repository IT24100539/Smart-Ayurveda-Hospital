import type { ReactNode } from "react";

type PageHeaderProps = {
  kicker: string;
  title: string;
  description: string;
  action?: ReactNode;
};

export function PageHeader({ kicker, title, description, action }: PageHeaderProps) {
  return (
    <section className="relative mb-6 overflow-hidden rounded-2xl bg-primary-dark text-white shadow-lg">
      <div className="pointer-events-none absolute -right-16 -top-20 h-56 w-56 rounded-full bg-white/10" aria-hidden="true" />
      <div className="pointer-events-none absolute bottom-0 left-0 h-1 w-full bg-gradient-to-r from-amber-300/80 via-amber-200/30 to-transparent" aria-hidden="true" />
      <div className="relative flex flex-col gap-4 p-6 md:flex-row md:items-end md:justify-between md:p-8">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-amber-100/80">{kicker}</p>
          <h1 className="mt-2 text-3xl text-white">{title}</h1>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-primary-muted">{description}</p>
        </div>
        {action ? <div className="shrink-0">{action}</div> : null}
      </div>
    </section>
  );
}
