import type { ButtonHTMLAttributes, HTMLAttributes } from "react";

type TabListProps = HTMLAttributes<HTMLDivElement> & {
  label: string;
};

export function TabList({ label, className = "", ...props }: TabListProps) {
  return <div role="tablist" aria-label={label} className={["flex flex-wrap gap-2", className].join(" ")} {...props} />;
}

type TabProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "role" | "aria-selected"> & {
  selected: boolean;
};

export function Tab({ selected, className = "", type = "button", ...props }: TabProps) {
  return (
    <button
      type={type}
      role="tab"
      aria-selected={selected}
      className={[
        "inline-flex min-h-11 items-center rounded-lg px-4 py-2 text-sm font-semibold transition-colors",
        selected
          ? "bg-primary text-primary-on"
          : "border border-field-border bg-surface-raised text-primary hover:border-primary hover:bg-primary-muted",
        className
      ].join(" ")}
      {...props}
    />
  );
}
