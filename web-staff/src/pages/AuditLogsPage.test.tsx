import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as auditApi from "../api/auditLogs";
import type { AuditLog } from "../api/auditLogs";
import { ApiError } from "../api/client";
import { AuditLogsPage } from "./AuditLogsPage";

vi.mock("../api/auditLogs", () => ({
  listAuditLogs: vi.fn()
}));

const row: AuditLog = {
  id: "log-1",
  actorUserId: "staff-1",
  actorEmail: "desk@hospital.local",
  actorRole: "FrontDeskStaff",
  action: "View",
  entityName: "Patient",
  entityId: "chart-1",
  targetUserId: "00000000-0000-0000-0000-000000000000",
  targetEmail: "",
  details: "Opened the patient chart",
  createdAt: "2026-10-01T10:00:00Z",
  ipAddress: "203.0.113.9"
};

const page = {
  items: [row],
  totalCount: 1,
  page: 1,
  pageSize: 20
};

describe("AuditLogsPage", () => {
  beforeEach(() => {
    vi.mocked(auditApi.listAuditLogs).mockReset();
    vi.mocked(auditApi.listAuditLogs).mockResolvedValue(page);
  });

  it("shows a loading state, then the log", async () => {
    let resolveList: (value: typeof page) => void = () => {};
    vi.mocked(auditApi.listAuditLogs).mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveList = resolve;
        })
    );

    render(<AuditLogsPage />);
    expect(screen.getByText("Loading audit log…")).toBeInTheDocument();
    resolveList(page);

    expect(await screen.findByText("desk@hospital.local")).toBeInTheDocument();
    expect(screen.getByText("Opened the patient chart")).toBeInTheDocument();
    expect(screen.getByText("203.0.113.9")).toBeInTheDocument();
    expect(screen.getByText("chart-1")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    vi.mocked(auditApi.listAuditLogs).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    render(<AuditLogsPage />);
    expect(await screen.findByText("No audit records found.")).toBeInTheDocument();
  });

  it("shows an error and retries", async () => {
    vi.mocked(auditApi.listAuditLogs)
      .mockRejectedValueOnce(new ApiError("The audit log is unavailable.", 503))
      .mockResolvedValue(page);

    render(<AuditLogsPage />);
    expect(await screen.findByText("The audit log is unavailable.")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(await screen.findByText("desk@hospital.local")).toBeInTheDocument();
  });

  it("applies filters and pages the result", async () => {
    vi.mocked(auditApi.listAuditLogs).mockResolvedValue({ ...page, totalCount: 21 });
    render(<AuditLogsPage />);
    await screen.findByText("desk@hospital.local");

    fireEvent.change(screen.getByLabelText("Search"), { target: { value: " desk@hospital.local " } });
    fireEvent.change(screen.getByLabelText("Action"), { target: { value: "View" } });
    fireEvent.change(screen.getByLabelText("Record type"), { target: { value: "Patient" } });
    fireEvent.change(screen.getByLabelText("Record id"), { target: { value: " chart-1 " } });
    fireEvent.change(screen.getByLabelText("From"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "2026-10-02" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));

    await waitFor(() => {
      expect(auditApi.listAuditLogs).toHaveBeenLastCalledWith(
        expect.objectContaining({
          query: "desk@hospital.local",
          action: "View",
          entityName: "Patient",
          entityId: "chart-1",
          page: 1,
          pageSize: 20
        })
      );
    });

    const applied = vi.mocked(auditApi.listAuditLogs).mock.calls.at(-1)?.[0];
    const from = new Date(applied?.fromDate ?? "");
    const to = new Date(applied?.toDate ?? "");
    expect(from.getFullYear()).toBe(2026);
    expect(from.getMonth()).toBe(9);
    expect(from.getDate()).toBe(1);
    expect(from.getHours()).toBe(0);
    expect(to.getDate()).toBe(2);
    expect(to.getHours()).toBe(23);

    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(auditApi.listAuditLogs).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
    });
  });

  it("keeps an invalid filter off the request", async () => {
    render(<AuditLogsPage />);
    await screen.findByText("desk@hospital.local");
    expect(auditApi.listAuditLogs).toHaveBeenCalledTimes(1);

    fireEvent.change(screen.getByLabelText("From"), { target: { value: "2026-10-05" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "2026-10-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    expect(screen.getByRole("alert")).toHaveTextContent("The end date must be on or after the start date.");
    expect(auditApi.listAuditLogs).toHaveBeenCalledTimes(1);

    fireEvent.change(screen.getByLabelText("From"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("To"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("Search"), { target: { value: "q".repeat(201) } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Search must be at most 200 characters.");

    fireEvent.change(screen.getByLabelText("Search"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("Record id"), { target: { value: "x".repeat(65) } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Record id must be at most 64 characters.");

    fireEvent.change(screen.getByLabelText("Record id"), { target: { value: "" } });
    fireEvent.change(screen.getByLabelText("From"), { target: { value: "1899-01-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Dates must be between 1900 and 2100.");
    expect(auditApi.listAuditLogs).toHaveBeenCalledTimes(1);
  });
});
