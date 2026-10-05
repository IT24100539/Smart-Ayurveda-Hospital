import { describe, expect, it } from "vitest";
import { buildCsv, eachIsoDate, validateExportRange } from "./csv";

describe("export dates and csv", () => {
  it("requires a bounded range", () => {
    expect(validateExportRange("", "2026-10-02")).toBe("Start date is required.");
    expect(validateExportRange("2026-10-02", "")).toBe("End date is required.");
    expect(validateExportRange("1899-01-01", "1899-01-02")).toBe("Dates must be between 1900 and 2100.");
    expect(validateExportRange("2026-10-03", "2026-10-02")).toBe("The end date must be on or after the start date.");
    expect(validateExportRange("2026-01-01", "2026-02-01")).toBe("Choose 31 days or fewer.");
    expect(validateExportRange("2026-01-01", "2026-01-31")).toBeNull();
  });

  it("lists each day and quotes cells that need it", () => {
    expect(eachIsoDate("2026-10-01", "2026-10-03")).toEqual(["2026-10-01", "2026-10-02", "2026-10-03"]);
    const csv = buildCsv(["Name", "Note"], [["Meera, Nair", 'Say "om"']]);
    expect(csv.startsWith("\uFEFF")).toBe(true);
    expect(csv).toContain('"Meera, Nair"');
    expect(csv).toContain('"Say ""om"""');
  });
});
