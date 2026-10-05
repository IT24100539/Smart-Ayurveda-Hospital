import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ComplaintSummary } from "../../api/feedback";
import { ComplaintQueuePage } from "./ComplaintQueuePage";

function complaint(overrides: Partial<ComplaintSummary> = {}): ComplaintSummary {
  return {
    id: "c-new",
    patientId: "patient-1",
    patientName: "Hiruni Silva",
    feedbackId: null,
    subject: "The abhyanga queue spilled into the corridor",
    description: "There was no update at reception.",
    priority: "Normal",
    status: "Open",
    assignedTo: null,
    escalatedAt: null,
    createdAt: "2026-09-28T09:30:00Z",
    isOverdue: false,
    ...overrides
  };
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" }
  });
}

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("ComplaintQueuePage", () => {
  it("defaults to every complaint, newest first, with a count on each chip", async () => {
    const fresh = complaint();
    const older = complaint({
      id: "c-old",
      subject: "Long wait before nadi pariksha",
      status: "Open",
      createdAt: "2026-09-20T09:00:00Z",
      isOverdue: true
    });
    const resolved = complaint({
      id: "c-done",
      subject: "Shirodhara aftercare was reviewed",
      status: "Resolved",
      createdAt: "2026-09-27T09:00:00Z",
      isOverdue: false
    });
    const urls: string[] = [];

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        urls.push(url);
        if (url.includes("/complaints/assignees")) {
          return json([]);
        }
        if (url.endsWith("/complaints") || url.includes("/complaints?")) {
          return json([older, resolved, fresh]);
        }
        throw new Error(`Unexpected request ${url}`);
      })
    );

    render(<ComplaintQueuePage />);

    expect(await screen.findByText(fresh.subject)).toBeInTheDocument();
    expect(screen.getByText(older.subject)).toBeInTheDocument();
    expect(screen.getByText(resolved.subject)).toBeInTheDocument();
    expect(urls.some((url) => url.includes("overdue=true"))).toBe(false);

    const subjects = screen.getAllByRole("heading", { level: 3 }).map((node) => node.textContent);
    expect(subjects).toEqual([fresh.subject, resolved.subject, older.subject]);

    expect(screen.getByRole("button", { name: "All (3)" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "Open (2)" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Overdue (1)" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Resolved (1)" })).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Overdue (1)" }));
    expect(screen.getByText(older.subject)).toBeInTheDocument();
    expect(screen.queryByText(fresh.subject)).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "All (3)" }));
    expect(screen.getByText(fresh.subject)).toBeInTheDocument();
  });
});
