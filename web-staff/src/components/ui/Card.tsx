import type { HTMLAttributes } from "react";

export function Card({ className = "", ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={["rounded-2xl border border-white/70 bg-surface-raised/95 shadow-[0_10px_30px_rgba(26,46,45,0.06)] backdrop-blur-sm", className].join(" ")}
      {...props}
    />
  );
}
