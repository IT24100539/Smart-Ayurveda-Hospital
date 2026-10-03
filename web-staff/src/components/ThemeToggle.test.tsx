import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { initTheme, THEME_STORAGE_KEY } from "../store/themeStore";
import { ThemeToggle } from "./ThemeToggle";

type Listener = () => void;

function mockSystemTheme(initialDark: boolean) {
  let dark = initialDark;
  const listeners = new Set<Listener>();
  window.matchMedia = vi.fn().mockImplementation((query: string) => ({
    get matches() {
      return query.includes("dark") && dark;
    },
    media: query,
    addEventListener: (_: string, listener: Listener) => listeners.add(listener),
    removeEventListener: (_: string, listener: Listener) => listeners.delete(listener)
  })) as unknown as typeof window.matchMedia;

  return {
    setDark(next: boolean) {
      dark = next;
      listeners.forEach((listener) => listener());
    }
  };
}

const htmlTheme = () => document.documentElement.dataset.theme;
const pressed = (name: string) => screen.getByRole("button", { name }).getAttribute("aria-pressed");

describe("theme toggle", () => {
  let stop: () => void = () => undefined;

  beforeEach(() => {
    window.localStorage.clear();
    delete document.documentElement.dataset.theme;
  });

  afterEach(() => {
    stop();
    cleanup();
  });

  it("defaults to the system setting when nothing is saved", () => {
    mockSystemTheme(true);
    stop = initTheme();
    render(<ThemeToggle />);

    expect(htmlTheme()).toBe("dark");
    expect(pressed("System")).toBe("true");
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
  });

  it("follows the system when it changes while set to system", () => {
    const system = mockSystemTheme(false);
    stop = initTheme();
    render(<ThemeToggle />);
    expect(htmlTheme()).toBe("light");

    act(() => system.setDark(true));
    expect(htmlTheme()).toBe("dark");
  });

  it("persists an explicit choice and stops following the system", () => {
    const system = mockSystemTheme(false);
    stop = initTheme();
    render(<ThemeToggle />);

    fireEvent.click(screen.getByRole("button", { name: "Dark" }));
    expect(htmlTheme()).toBe("dark");
    expect(pressed("Dark")).toBe("true");
    expect(pressed("System")).toBe("false");
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");

    act(() => system.setDark(false));
    expect(htmlTheme()).toBe("dark");
  });

  it("restores the saved choice on the next load", () => {
    mockSystemTheme(true);
    window.localStorage.setItem(THEME_STORAGE_KEY, "light");
    stop = initTheme();
    render(<ThemeToggle />);

    expect(htmlTheme()).toBe("light");
    expect(pressed("Light")).toBe("true");
  });

  it("ignores an invalid saved value", () => {
    mockSystemTheme(false);
    window.localStorage.setItem(THEME_STORAGE_KEY, "sepia");
    stop = initTheme();
    render(<ThemeToggle />);

    expect(htmlTheme()).toBe("light");
    expect(pressed("System")).toBe("true");
  });
});
