export const MAX_EXPORT_DAYS = 31;

export function validateExportRange(fromDate: string, toDate: string): string | null {
  if (!fromDate) return "Start date is required.";
  if (!toDate) return "End date is required.";
  if (yearOutOfRange(fromDate) || yearOutOfRange(toDate)) {
    return "Dates must be between 1900 and 2100.";
  }
  if (toDate < fromDate) {
    return "The end date must be on or after the start date.";
  }
  if (inclusiveDayCount(fromDate, toDate) > MAX_EXPORT_DAYS) {
    return `Choose ${MAX_EXPORT_DAYS} days or fewer.`;
  }
  return null;
}

export function eachIsoDate(fromDate: string, toDate: string): string[] {
  const dates: string[] = [];
  const cursor = utcDate(fromDate);
  const end = utcDate(toDate);
  while (cursor.getTime() <= end.getTime()) {
    dates.push(isoFromUtc(cursor));
    cursor.setUTCDate(cursor.getUTCDate() + 1);
  }
  return dates;
}

export function localIsoDate(value: string): string {
  if (/^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

export function buildCsv(headers: string[], rows: string[][]): string {
  const lines = [headers, ...rows].map((row) => row.map(escapeCell).join(","));
  return `\uFEFF${lines.join("\r\n")}`;
}

export function downloadCsv(fileName: string, csv: string): void {
  const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(url);
}

function yearOutOfRange(value: string): boolean {
  const year = Number(value.slice(0, 4));
  return !Number.isInteger(year) || year < 1900 || year > 2100;
}

function inclusiveDayCount(fromDate: string, toDate: string): number {
  const ms = utcDate(toDate).getTime() - utcDate(fromDate).getTime();
  return Math.round(ms / 86_400_000) + 1;
}

function utcDate(isoDate: string): Date {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

function isoFromUtc(date: Date): string {
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  const day = String(date.getUTCDate()).padStart(2, "0");
  return `${date.getUTCFullYear()}-${month}-${day}`;
}

function escapeCell(value: string): string {
  if (/[",\r\n]/.test(value)) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}
