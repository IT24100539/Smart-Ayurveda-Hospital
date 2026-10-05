import type { ButtonHTMLAttributes } from "react";

const variants = {
  primary: "bg-primary text-primary-on hover:bg-primary-dark",
  secondary: "border border-field-border bg-surface-raised text-primary hover:border-primary hover:bg-primary-muted",
  ghost: "bg-transparent text-primary hover:bg-primary-muted",
  danger: "bg-danger text-danger-on hover:opacity-90"
} as const;

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: keyof typeof variants;
  loading?: boolean;
};

export function Button({
  variant = "primary",
  loading = false,
  className = "",
  type = "button",
  disabled,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={[
        "inline-flex min-h-11 items-center justify-center gap-2 rounded-full px-4 py-2 text-sm font-semibold transition-colors",
        "disabled:cursor-not-allowed disabled:opacity-60",
        variants[variant],
        className
      ].join(" ")}
      {...props}
    >
      {loading ? (
        <span className="inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" aria-hidden="true" />
      ) : null}
      {children}
    </button>
  );
}
