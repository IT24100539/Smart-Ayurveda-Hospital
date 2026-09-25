import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { WorkflowExecution, WorkflowPage } from "../api/workflows";
import { AiApprovalsPage } from "./AiApprovalsPage";

const workflowId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function workflow(overrides: Partial<WorkflowExecution> = {}): WorkflowExecution {
  return {
    id: workflowId,
    agentName: "scheduling_bed",
    objectiveText: "Admit the patient for inpatient panchakarma.",
    plan: ["check_schedule", "check_ward", "propose"],
    completedSteps: ["check_schedule"],
    toolResults: [
      {
        tool: "check_ward_availability",
        succeeded: true,
        output: { ward_id: "ward-1" },
        calledAt: "2026-09-25T04:00:00Z"
      }
    ],
    validationResults: [
      { check: "scheduling_and_bed", passed: true, detail: "Ward has a free bed." },
      { check: "schedule", passed: false, detail: "Date is closed." }
    ],
    errors: [],
    approvalStatus: "Pending",
    finalOutcome: "Success",
    relatedEntityType: "AdmissionRequest",
    relatedEntityId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    createdAt: "2026-09-25T03:00:00Z",
    updatedAt: "2026-09-25T04:00:00Z",
    ...overrides
  };
}

function pageOf(items: WorkflowExecution[]): WorkflowPage {
  return { items, totalCount: items.length, page: 1, pageSize: 50 };
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

describe("AiApprovalsPage", () => {
  it("shows the plan on the left and the log with decisions on the right", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/workflow-executions")) return json(pageOf([workflow()]));
        if (url.includes(`/agent-workflows/${workflowId}`) && !url.includes("approve")) return json(workflow());
        throw new Error(`Unexpected request ${url}`);
      })
    );

    render(<AiApprovalsPage />);

    expect(await screen.findByText("check_schedule")).toBeInTheDocument();
    expect(screen.getByText("check_ward")).toBeInTheDocument();
    expect(screen.getByText("check_ward_availability")).toBeInTheDocument();
    expect(screen.getByText("Pass: scheduling_and_bed")).toBeInTheDocument();
    expect(screen.getByText("Fail: schedule")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Request revision" })).toBeEnabled();
  });

  it("refetches after approve and shows the bed allocation", async () => {
    const approved = workflow({ approvalStatus: "Approved" });
    let decided = false;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/approve") && init?.method === "PATCH") {
        decided = true;
        return json(approved);
      }
      if (url.includes("/wards")) {
        return json([
          {
            id: "ward-1",
            beds: [
              { id: "bed-1", bedLabel: "A-05", isOccupied: decided },
              { id: "bed-2", bedLabel: "A-06", isOccupied: true }
            ]
          }
        ]);
      }
      if (url.includes(`/agent-workflows/${workflowId}`)) return json(decided ? approved : workflow());
      if (url.includes("/workflow-executions")) return json(pageOf([workflow()]));
      throw new Error(`Unexpected request ${url}`);
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<AiApprovalsPage />);
    fireEvent.click(await screen.findByRole("button", { name: "Approve" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Bed A-05 allocated");
    const approveCall = fetchMock.mock.calls.find((call) => String(call[0]).includes("/approve"));
    expect(approveCall?.[1]).toMatchObject({ method: "PATCH" });
  });
});
