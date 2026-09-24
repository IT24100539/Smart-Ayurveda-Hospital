import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { FeedbackDetail, FeedbackSummary, PagedResult, Reply } from "../../api/feedback";
import { useAuthStore } from "../../store/authStore";
import { FeedbackDashboardPage } from "./FeedbackDashboardPage";

const feedbackId = "11111111-1111-1111-1111-111111111111";
const namedId = "33333333-3333-3333-3333-333333333333";
const replyId = "22222222-2222-2222-2222-222222222222";

function summary(overrides: Partial<FeedbackSummary> = {}): FeedbackSummary {
  return {
    id: feedbackId,
    patientId: null,
    patientName: "Lakshmi Iyer",
    appointmentId: null,
    treatmentId: null,
    rating: 2,
    comment: "The shirodhara room was cold after the therapy.",
    isAnonymous: true,
    sentiment: "Negative",
    category: "FacilityIssue",
    status: "PendingModeration",
    likeCount: 0,
    dislikeCount: 0,
    postedReplyCount: 0,
    createdAt: "2026-09-23T08:00:00Z",
    ...overrides
  };
}

function detailFor(item: FeedbackSummary, replies: Reply[] = []): FeedbackDetail {
  return {
    id: item.id,
    patientId: item.patientId,
    patientName: item.isAnonymous ? "Anonymous patient" : item.patientName,
    appointmentId: item.appointmentId,
    treatmentId: item.treatmentId,
    rating: item.rating,
    comment: item.comment,
    isAnonymous: item.isAnonymous,
    sentiment: item.sentiment,
    category: item.category,
    status: item.status,
    moderatedBy: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    moderatedAt: "2026-09-23T09:00:00Z",
    likeCount: item.likeCount,
    dislikeCount: item.dislikeCount,
    replies,
    createdAt: item.createdAt,
    updatedAt: "2026-09-23T09:00:00Z"
  };
}

function pageOf(items: FeedbackSummary[]): PagedResult<FeedbackSummary> {
  return { items, totalCount: items.length, page: 1, pageSize: 10 };
}

function draftReply(text = "Namaste. We are sorry the wait for abhyanga ran long."): Reply {
  return {
    id: replyId,
    feedbackId,
    userId: null,
    userRole: "Staff",
    reply: text,
    isAiGenerated: true,
    status: "Draft",
    createdAt: "2026-09-23T10:00:00Z"
  };
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" }
  });
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((res) => {
    resolve = res;
  });
  return { promise, resolve };
}

beforeEach(() => {
  localStorage.clear();
  useAuthStore.setState({ token: null, user: null });
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});

describe("FeedbackDashboardPage", () => {
  it("renders anonymous feedback as Anonymous patient", async () => {
    const anonymous = summary();
    const named = summary({
      id: namedId,
      patientId: "44444444-4444-4444-4444-444444444444",
      patientName: "Anita Deshmukh",
      comment: "Abhyanga was calming and the oil was warm.",
      isAnonymous: false,
      sentiment: "Positive",
      category: "TreatmentQuality",
      rating: 5,
      status: "Visible"
    });

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/feedback/staff")) {
          return json(pageOf([anonymous, named]));
        }
        throw new Error(`Unexpected request ${url}`);
      })
    );

    render(<FeedbackDashboardPage />);

    expect((await screen.findAllByText("Anonymous patient")).length).toBeGreaterThan(0);
    expect(screen.getAllByText("Anita Deshmukh").length).toBeGreaterThan(0);
    expect(screen.queryByText("Lakshmi Iyer")).not.toBeInTheDocument();
    expect(screen.getByText("The shirodhara room was cold after the therapy.")).toBeInTheDocument();
  });

  it("shows a loading state, then approves an AI draft", async () => {
    const item = summary({
      comment: "The abhyanga wait ran long today and the slot started late."
    });
    const draft = draftReply();
    const gate = deferred<Response>();
    const calls: { url: string; method: string; body: unknown }[] = [];

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = (init?.method ?? "GET").toUpperCase();
        const body = init?.body ? JSON.parse(String(init.body)) : undefined;
        calls.push({ url, method, body });

        if (url.includes("/feedback/staff")) {
          return json(pageOf([item]));
        }
        if (url.includes("/replies/ai-draft")) {
          return gate.promise;
        }
        if (url.includes("/decision")) {
          return json({ ...draft, status: "Posted", userId: "staff-1" });
        }
        if (method === "GET" && url.includes(`/feedback/${feedbackId}`)) {
          return json(detailFor(item));
        }
        throw new Error(`Unexpected request ${method} ${url}`);
      })
    );

    render(<FeedbackDashboardPage />);
    await screen.findByText(/abhyanga wait ran long/i);
    fireEvent.click(screen.getByRole("button", { name: /expand feedback from anonymous patient/i }));
    fireEvent.click(await screen.findByRole("button", { name: "Generate Reply" }));

    expect(await screen.findByRole("button", { name: /generating reply/i })).toBeDisabled();

    gate.resolve(json(draft, 201));

    expect(await screen.findByDisplayValue(draft.reply)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save edit" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve & Post" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject" })).toBeInTheDocument();
    expect(screen.getByText("AI")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Approve & Post" }));

    expect(await screen.findByText("Posted")).toBeInTheDocument();
    await waitFor(() => {
      expect(calls.some((call) => call.url.includes(`/replies/${replyId}/decision`) && call.method === "PATCH")).toBe(
        true
      );
    });
    const decision = calls.find((call) => call.url.includes("/decision"));
    expect(decision?.body).toEqual({ decision: "Approve" });
  });

  it("rejects an AI draft", async () => {
    const item = summary({ comment: "Waiting for the nadi pariksha took most of the morning." });
    const draft = draftReply("Namaste. We will review the morning queue for nadi pariksha.");
    const calls: { url: string; method: string; body: unknown }[] = [];

    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = (init?.method ?? "GET").toUpperCase();
        const body = init?.body ? JSON.parse(String(init.body)) : undefined;
        calls.push({ url, method, body });

        if (url.includes("/feedback/staff")) {
          return json(pageOf([item]));
        }
        if (url.includes("/replies/ai-draft")) {
          return json(draft, 201);
        }
        if (url.includes("/decision")) {
          return json({ ...draft, status: "Rejected" });
        }
        if (method === "GET" && url.includes(`/feedback/${feedbackId}`)) {
          return json(detailFor(item));
        }
        throw new Error(`Unexpected request ${method} ${url}`);
      })
    );

    render(<FeedbackDashboardPage />);
    await screen.findByText(/nadi pariksha/i);
    fireEvent.click(screen.getByRole("button", { name: /expand feedback from anonymous patient/i }));
    fireEvent.click(await screen.findByRole("button", { name: "Generate Reply" }));
    fireEvent.click(await screen.findByRole("button", { name: "Reject" }));

    expect(await screen.findByText("Rejected")).toBeInTheDocument();
    await waitFor(() => {
      const decision = calls.find((call) => call.url.includes("/decision"));
      expect(decision?.method).toBe("PATCH");
      expect(decision?.body).toEqual({ decision: "Reject" });
    });
    expect(screen.queryByRole("button", { name: "Approve & Post" })).not.toBeInTheDocument();
  });
});
