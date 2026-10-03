import type { ReactNode } from "react";
import { Badge, type BadgeTone } from "./Badge";
import { Button } from "./Button";

export function SectionRow({ title, action }: { title: string; action?: ReactNode }) {
  return (
    <div className="flex items-end justify-between gap-4">
      <h2 className="font-display text-section text-heading">{title}</h2>
      {action}
    </div>
  );
}

export function TextLink({ children }: { children: ReactNode }) {
  return (
    <span className="inline-flex items-center gap-1 text-sm font-semibold text-primary">
      {children}
      <span aria-hidden="true">→</span>
    </span>
  );
}

const statusIcon: Record<string, BadgeTone> = {
  Approved: "approved",
  Pending: "pending",
  Rejected: "rejected",
  Cancelled: "error",
  Completed: "success"
};

export function StatusPill({ status }: { status: string }) {
  const tone = statusIcon[status] ?? "neutral";
  return <Badge tone={tone}>{status}</Badge>;
}

export function UnderlineTabs({
  tabs,
  selected,
  onSelect
}: {
  tabs: { id: string; label: string; count?: number }[];
  selected: string;
  onSelect: (id: string) => void;
}) {
  return (
    <div className="border-b border-surface-border">
      <div role="tablist" className="flex gap-6 overflow-x-auto [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
        {tabs.map((tab) => {
          const active = tab.id === selected;
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={active}
              onClick={() => onSelect(tab.id)}
              className={[
                "inline-flex min-h-12 shrink-0 items-center gap-2 border-b-2 px-1 text-sm font-semibold",
                active ? "border-primary text-primary" : "border-transparent text-muted"
              ].join(" ")}
            >
              {tab.label}
              {tab.count !== undefined ? (
                <span className="rounded-full bg-primary px-2 py-0.5 text-label text-primary-on">{tab.count}</span>
              ) : null}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export function InfoCard({
  kicker,
  title,
  status,
  facts,
  action
}: {
  kicker: string;
  title: string;
  status: string;
  facts: { label: string; value: string }[];
  action?: ReactNode;
}) {
  return (
    <article className="rounded-card border border-surface-border bg-surface-raised p-6 shadow-card">
      <div className="flex items-start justify-between gap-3">
        <p className="text-label font-semibold uppercase text-label-muted">{kicker}</p>
        <StatusPill status={status} />
      </div>
      <h3 className="mt-3 font-display text-card text-heading">{title}</h3>
      <hr className="my-4 border-surface-border" />
      <dl className="grid gap-4 sm:grid-cols-2">
        {facts.map((fact) => (
          <div key={fact.label}>
            <dt className="text-label font-semibold uppercase text-label-muted">{fact.label}</dt>
            <dd className="mt-1 text-body text-ink">{fact.value}</dd>
          </div>
        ))}
      </dl>
      {action ? <div className="mt-4 flex justify-end">{action}</div> : null}
    </article>
  );
}

export function Stepper({ labels, current }: { labels: string[]; current: number }) {
  return (
    <ol className="grid grid-cols-4 gap-2">
      {labels.map((label, index) => {
        const done = index < current;
        const active = index === current;
        return (
          <li key={label} className="text-center">
            <div className="flex items-center">
              <span className={index === 0 ? "h-px flex-1" : "h-px flex-1 bg-surface-border"} />
              <span
                className={[
                  "flex h-12 w-12 items-center justify-center rounded-full border text-sm font-semibold",
                  active || done ? "border-primary bg-primary text-primary-on" : "border-surface-border bg-surface-sunken text-muted"
                ].join(" ")}
                aria-current={active ? "step" : undefined}
              >
                {done ? "✓" : index + 1}
              </span>
              <span className={index === labels.length - 1 ? "h-px flex-1" : "h-px flex-1 bg-surface-border"} />
            </div>
            <p className="mt-2 text-label font-semibold uppercase text-label-muted">{label}</p>
          </li>
        );
      })}
    </ol>
  );
}

export function BookingOption({
  name,
  price,
  description,
  duration,
  category,
  selected
}: {
  name: string;
  price: string;
  description: string;
  duration: string;
  category: string;
  selected?: boolean;
}) {
  return (
    <article
      className={[
        "rounded-card border bg-surface-raised p-5",
        selected ? "border-primary" : "border-surface-border"
      ].join(" ")}
    >
      <div className="flex items-start justify-between gap-3">
        <h3 className="font-display text-card text-heading">{name}</h3>
        <p className="font-display text-card text-primary">{price}</p>
      </div>
      <p className="mt-2 text-body text-muted">{description}</p>
      <div className="mt-4 flex items-center justify-between gap-3">
        <p className="text-caption text-muted">◷ {duration}</p>
        <span className="rounded-full bg-pill-bg px-3 py-1 text-label font-semibold uppercase text-pill-fg">{category}</span>
      </div>
    </article>
  );
}

export function KpiCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="rounded-card border border-surface-border bg-surface-raised p-6 shadow-card">
      <p className="text-label font-semibold uppercase text-label-muted">{label}</p>
      <p className="mt-2 font-display text-display text-heading">{value}</p>
    </article>
  );
}

export function ChatPanel() {
  return (
    <section className="rounded-header border border-surface-border bg-surface-raised p-5" aria-label="Sample conversation">
      <div className="flex items-center gap-3">
        <span className="flex h-12 w-12 items-center justify-center rounded-full bg-primary-muted font-display text-primary" aria-hidden="true">
          C
        </span>
        <div className="min-w-0 flex-1">
          <h2 className="font-display text-card text-heading">Charaka</h2>
          <StatusPill status="Ready" />
        </div>
        <Button variant="secondary">New chat</Button>
      </div>
      <div className="mt-4 space-y-3 rounded-card bg-surface-sunken p-4">
        <p className="max-w-[80%] rounded-card bg-surface-raised px-4 py-3 text-body text-ink">Ask about a therapy from the catalogue.</p>
        <p className="ml-auto max-w-[80%] rounded-card bg-primary px-4 py-3 text-body text-primary-on">Which therapies are scheduled today?</p>
      </div>
      <p className="mt-4 text-label font-semibold uppercase text-label-muted">Suggested inquiries</p>
    </section>
  );
}
