import { create } from "zustand";

export type ThemePreference = "light" | "dark" | "system";
export type ResolvedTheme = "light" | "dark";

export const THEME_STORAGE_KEY = "sah-staff-theme";

const DARK_QUERY = "(prefers-color-scheme: dark)";

export function isThemePreference(value: unknown): value is ThemePreference {
  return value === "light" || value === "dark" || value === "system";
}

export function readStoredPreference(): ThemePreference {
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    return isThemePreference(stored) ? stored : "system";
  } catch {
    return "system";
  }
}

function systemPrefersDark(): boolean {
  return typeof window.matchMedia === "function" && window.matchMedia(DARK_QUERY).matches;
}

export function resolveTheme(preference: ThemePreference): ResolvedTheme {
  if (preference === "system") {
    return systemPrefersDark() ? "dark" : "light";
  }
  return preference;
}

function applyTheme(preference: ThemePreference): void {
  const root = document.documentElement;
  root.dataset.theme = resolveTheme(preference);
  root.dataset.themePreference = preference;
}

type ThemeState = {
  preference: ThemePreference;
  setPreference: (preference: ThemePreference) => void;
};

export const useThemeStore = create<ThemeState>()((set) => ({
  preference: "system",
  setPreference: (preference) => {
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, preference);
    } catch {
      // Storage can be blocked (private mode); the choice still applies for this session.
    }
    applyTheme(preference);
    set({ preference });
  }
}));

/**
 * Reads the saved choice (default: follow the system), applies it to <html>, and
 * keeps "system" in sync with the operating system setting. Returns a cleanup function.
 */
export function initTheme(): () => void {
  const preference = readStoredPreference();
  useThemeStore.setState({ preference });
  applyTheme(preference);

  if (typeof window.matchMedia !== "function") {
    return () => undefined;
  }

  const query = window.matchMedia(DARK_QUERY);
  const onChange = () => {
    if (useThemeStore.getState().preference === "system") {
      applyTheme("system");
    }
  };
  query.addEventListener("change", onChange);
  return () => query.removeEventListener("change", onChange);
}
