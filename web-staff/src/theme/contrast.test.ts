import { describe, expect, it } from "vitest";

/**
 * Reads the theme tokens straight from index.css so the test cannot drift from the real palette.
 * The project has no @types/node, so fs is loaded dynamically; `npm test` runs from web-staff/.
 */
const { readFileSync } = (await import(/* @vite-ignore */ ["node", "fs"].join(":"))) as {
  readFileSync(path: string, encoding: "utf8"): string;
};
const css = readFileSync("src/index.css", "utf8");

type Rgb = [number, number, number];

function readTokens(selector: string): Record<string, Rgb> {
  const start = css.indexOf(selector);
  const block = css.slice(css.indexOf("{", start) + 1, css.indexOf("\n}", start));
  const tokens: Record<string, Rgb> = {};
  for (const match of block.matchAll(/--([\w-]+):\s*(\d+) (\d+) (\d+);/g)) {
    tokens[match[1]!] = [Number(match[2]), Number(match[3]), Number(match[4])];
  }
  return tokens;
}

const themes = {
  light: readTokens(":root,"),
  dark: readTokens(':root[data-theme="dark"]')
};

function luminance([r, g, b]: Rgb): number {
  const channel = (value: number) => {
    const c = value / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

function ratio(a: Rgb, b: Rgb): number {
  const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (hi! + 0.05) / (lo! + 0.05);
}

/** foreground, background. 4.5 for text, 3 for UI boundaries and focus indicators. */
const textPairs: [string, string][] = [
  ["ink", "surface"],
  ["ink", "surface-raised"],
  ["ink", "surface-sunken"],
  ["ink", "field-bg"],
  ["muted", "surface"],
  ["muted", "surface-raised"],
  ["muted", "surface-sunken"],
  ["muted", "field-bg"],
  ["muted", "primary-muted"],
  ["label", "surface"],
  ["label", "surface-raised"],
  ["label", "surface-sunken"],
  ["heading", "surface"],
  ["heading", "surface-raised"],
  ["heading", "primary-muted"],
  ["primary", "surface"],
  ["primary", "surface-raised"],
  ["primary", "primary-muted"],
  ["on-primary", "primary"],
  ["on-primary", "primary-dark"],
  ["on-danger", "danger"],
  ["danger", "surface"],
  ["danger", "surface-raised"],
  ["danger", "status-error-bg"],
  ["warning", "surface"],
  ["warning", "surface-raised"],
  ["warning", "surface-sunken"],
  ["success", "surface"],
  ["success", "surface-raised"],
  ["gold", "gold-muted"],
  ["gold", "surface"],
  ["gold", "surface-raised"],
  ["pill-fg", "pill-bg"],
  ["status-pending-fg", "status-pending-bg"],
  ["status-approved-fg", "status-approved-bg"],
  ["status-rejected-fg", "status-rejected-bg"],
  ["status-success-fg", "status-success-bg"],
  ["status-error-fg", "status-error-bg"],
  ["status-error-fg", "surface-raised"],
  ["on-header", "header-start"],
  ["on-header", "header-end"],
  ["on-scrim", "scrim"],
  ["on-hero", "hero"],
  ["hero-muted", "hero"],
  ["hero-accent", "hero"],
  ["hero-deep", "hero-button"],
  ["on-hero", "hero-deep"]
];

const boundaryPairs: [string, string][] = [
  ["field-border", "field-bg"],
  ["field-border", "surface-raised"],
  ["field-border", "surface-sunken"],
  ["focus", "surface"],
  ["focus", "surface-raised"]
];

describe.each(["light", "dark"] as const)("%s theme meets WCAG AA", (name) => {
  const tokens = themes[name];

  it.each(textPairs)("text %s on %s is at least 4.5:1", (fg, bg) => {
    expect(tokens[fg], `missing --${fg}`).toBeDefined();
    expect(tokens[bg], `missing --${bg}`).toBeDefined();
    expect(ratio(tokens[fg]!, tokens[bg]!)).toBeGreaterThanOrEqual(4.5);
  });

  it.each(boundaryPairs)("%s against %s is at least 3:1", (fg, bg) => {
    expect(ratio(tokens[fg]!, tokens[bg]!)).toBeGreaterThanOrEqual(3);
  });
});

describe("measured header gradient contrast", () => {
  it("light header text is 6.00:1 on the start and 4.95:1 on the end", () => {
    const tokens = themes.light;
    const start = ratio(tokens["on-header"]!, tokens["header-start"]!);
    const end = ratio(tokens["on-header"]!, tokens["header-end"]!);
    expect(Number(start.toFixed(2))).toBe(6);
    expect(Number(end.toFixed(2))).toBe(4.95);
  });

  it("dark header text is 7.17:1 on the start and 4.95:1 on the end", () => {
    const tokens = themes.dark;
    const start = ratio(tokens["on-header"]!, tokens["header-start"]!);
    const end = ratio(tokens["on-header"]!, tokens["header-end"]!);
    expect(Number(start.toFixed(2))).toBe(7.17);
    expect(Number(end.toFixed(2))).toBe(4.95);
  });
});

describe("surfaces that stay dark in both themes", () => {
  it("focus rings on dark panels are white", () => {
    const hero = themes.light.hero!;
    const onHero = themes.light["on-hero"]!;
    expect(css).toMatch(/\.on-dark\s*{\s*--focus: 255 255 255;/);
    expect(ratio(onHero, hero)).toBeGreaterThanOrEqual(3);
  });
});
