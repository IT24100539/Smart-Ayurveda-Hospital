import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "./client";
import { collectAppointments, collectInvoices } from "./exports";
import type { Invoice } from "./invoices";

vi.mock("./client", () => ({
  api: { request: vi.fn() }
}));

function midday(isoDate: string): string {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, month - 1, day, 12, 0, 0, 0).toISOString();
}

function invoice(number: string, createdOn: string): Invoice {
  const createdAt = midday(createdOn);
  return {
    id: number,
    invoiceNumber: number,
    patientId: "11111111-1111-1111-1111-111111111111",
    currency: "LKR",
    status: "Issued",
    total: 1500,
    amountPaid: 0,
    balance: 1500,
    createdByUserId: "user-1",
    createdAt,
    updatedAt: createdAt,
    issuedAt: createdAt,
    paidAt: null,
    cancelledAt: null,
    notes: null,
    lines: [],
    payments: []
  };
}

describe("collect export rows", () => {
  beforeEach(() => {
    vi.mocked(api.request).mockReset();
  });

  it("pages each visit day until that day's count is covered", async () => {
    vi.mocked(api.request).mockImplementation(async (path: string) => {
      const url = new URL(path, "http://localhost");
      const date = url.searchParams.get("date");
      const page = url.searchParams.get("page");
      if (date === "2026-10-01" && page === "1") {
        return {
          items: [{ id: "a1", patientName: "Meera", patientUhid: "UH1", treatmentName: "Abhyanga", requestedDate: date, requestedTimeSlot: "Morning", status: "Approved" }],
          totalCount: 2,
          page: 1,
          pageSize: 100
        };
      }
      if (date === "2026-10-01" && page === "2") {
        return {
          items: [{ id: "a2", patientName: "Arun", patientUhid: "UH2", treatmentName: "Shirodhara", requestedDate: date, requestedTimeSlot: "Evening", status: "Completed" }],
          totalCount: 2,
          page: 2,
          pageSize: 100
        };
      }
      return { items: [], totalCount: 0, page: 1, pageSize: 100 };
    });

    const rows = await collectAppointments("2026-10-01", "2026-10-02");
    expect(rows.map((row) => row.id)).toEqual(["a1", "a2"]);
    expect(vi.mocked(api.request)).toHaveBeenCalledTimes(3);
  });

  it("keeps bills raised in the range and stops once older bills appear", async () => {
    const page = {
      items: [invoice("INV-3", "2026-10-04"), invoice("INV-2", "2026-10-02"), invoice("INV-1", "2026-09-30")],
      totalCount: 300,
      page: 1,
      pageSize: 100
    };
    vi.mocked(api.request).mockResolvedValue(page);

    const rows = await collectInvoices("2026-10-01", "2026-10-03");
    expect(rows.map((row) => row.invoiceNumber)).toEqual(["INV-2"]);
    expect(vi.mocked(api.request)).toHaveBeenCalledTimes(1);
  });
});
