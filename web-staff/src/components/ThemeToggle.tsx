import type { ReactNode } from "react";
import { useThemeStore, type ThemePreference } from "../store/themeStore";

function Glyph({ children }: { children: ReactNode }) {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className="h-4 w-4" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {children}
    </svg>
  );
}

const OPTIONS: { value: ThemePreference; label: string; icon: ReactNode }[] = [
  {
    value: "light",
    label: "Light",
    icon: (
      <Glyph>
        <circle cx="12" cy="12" r="4" />
        <path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6 7 7M17 17l1.4 1.4M5.6 18.4 7 17M17 7l1.4-1.4" />
      </Glyph>
    )
  },
  {
    value: "system",
    label: "System",
    icon: (
      <Glyph>
        <rect x="3" y="4" width="18" height="12" rx="2" />
        <path d="M8 20h8M12 16v4" />
      </Glyph>
    )
  },
  {
    value: "dark",
    label: "Dark",
    icon: (
      <Glyph>
        <path d="M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5Z" />
      </Glyph>
    )
  }
];

export function ThemeToggle() {
  const preference = useThemeStore((state) => state.preference);
  const setPreference = useThemeStore((state) => state.setPreference);

  return (
    <div role="group" aria-label="Colour theme" className="inline-flex rounded-full border border-field-border bg-surface p-0.5">
      {OPTIONS.map((option) => {
        const selected = preference === option.value;
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={selected}
            title={option.label}
            onClick={() => setPreference(option.value)}
            className={[
              "inline-flex h-10 w-10 items-center justify-center rounded-full transition-colors",
              selected ? "bg-primary text-primary-on" : "text-muted hover:bg-primary-muted hover:text-primary"
            ].join(" ")}
          >
            {option.icon}
            <span className="sr-only">{option.label}</span>
          </button>
        );
      })}
    </div>
  );
}
