import type { HTMLAttributes } from "react";

export function Card({ className = "", ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={["rounded-card border border-surface-border bg-surface-raised text-ink shadow-card", className].join(" ")}
      {...props}
    />
  );
}
