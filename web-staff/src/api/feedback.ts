import { ApiError, api } from "./client";

export type FeedbackStatus = "Visible" | "Hidden" | "PendingModeration";
export type FeedbackCategory =
  | "TreatmentQuality"
  | "WaitingTime"
  | "StaffService"
  | "FacilityIssue"
  | "Other";
export type FeedbackSentiment = "Positive" | "Neutral" | "Negative";
export type FeedbackReplyStatus = "Draft" | "Approved" | "Rejected" | "Posted";
export type ReplyDecision = "Approve" | "Edit" | "Reject" | "Save";
export type ModerationAction = "Show" | "Hide";
export type ComplaintStatus = "Open" | "InProgress" | "Escalated" | "Resolved";
export type ComplaintPriority = "Normal" | "High";

export type Reply = {
  id: string;
  feedbackId: string;
  userId: string | null;
  userRole: "Staff" | "Patient";
  reply: string;
  isAiGenerated: boolean;
  status: FeedbackReplyStatus;
  createdAt: string;
};

export type FeedbackSummary = {
  id: string;
  patientId: string | null;
  patientName: string;
  appointmentId: string | null;
  treatmentId: string | null;
  rating: number;
  comment: string;
  isAnonymous: boolean;
  sentiment: FeedbackSentiment | null;
  category: FeedbackCategory | null;
  status: FeedbackStatus;
  likeCount: number;
  dislikeCount: number;
  postedReplyCount: number;
  createdAt: string;
};

export type FeedbackDetail = {
  id: string;
  patientId: string | null;
  patientName: string;
  appointmentId: string | null;
  treatmentId: string | null;
  rating: number;
  comment: string;
  isAnonymous: boolean;
  sentiment: FeedbackSentiment | null;
  category: FeedbackCategory | null;
  status: FeedbackStatus;
  moderatedBy: string | null;
  moderatedAt: string | null;
  likeCount: number;
  dislikeCount: number;
  replies: Reply[];
  createdAt: string;
  updatedAt: string;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type ComplaintSummary = {
  id: string;
  patientId: string;
  patientName: string;
  feedbackId: string | null;
  subject: string;
  description: string;
  priority: ComplaintPriority;
  status: ComplaintStatus;
  assignedTo: string | null;
  escalatedAt: string | null;
  createdAt: string;
  isOverdue: boolean;
};

export type FeedbackListQuery = {
  status?: FeedbackStatus | "";
  rating?: number | "";
  category?: FeedbackCategory | "";
  sentiment?: FeedbackSentiment | "";
  page: number;
  pageSize: number;
  sort?: string;
  search?: string;
};

export type FeedbackStats = {
  total: number;
  averageRating: number;
  pendingModeration: number;
  negative: number;
};

export type StaffAlert = {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
};

export type StaffAssignee = {
  id: string;
  fullName: string;
  role: string;
};

export function errorMessage(err: unknown, fallback: string): string {
  if (err instanceof ApiError) {
    return err.message;
  }
  return fallback;
}

export function listStaffFeedback(query: FeedbackListQuery): Promise<PagedResult<FeedbackSummary>> {
  const params = new URLSearchParams();
  if (query.status) {
    params.set("status", query.status);
  }
  if (query.rating) {
    params.set("rating", String(query.rating));
  }
  if (query.category) {
    params.set("category", query.category);
  }
  if (query.sentiment) {
    params.set("sentiment", query.sentiment);
  }
  params.set("page", String(query.page));
  params.set("pageSize", String(query.pageSize));
  if (query.sort) {
    params.set("sort", query.sort);
  }
  if (query.search?.trim()) {
    params.set("search", query.search.trim());
  }
  return api.request<PagedResult<FeedbackSummary>>(`/feedback/staff?${params.toString()}`);
}

export function getFeedbackStats(): Promise<FeedbackStats> {
  return api.request<FeedbackStats>("/feedback/summary");
}

export function listStaffAlerts(): Promise<StaffAlert[]> {
  return api.request<StaffAlert[]>("/notifications/staff");
}

export function getStaffFeedback(id: string): Promise<FeedbackDetail> {
  return api.request<FeedbackDetail>(`/feedback/${id}`);
}

export function moderateFeedback(id: string, action: ModerationAction): Promise<FeedbackDetail> {
  return api.request<FeedbackDetail>(`/feedback/${id}/moderate`, {
    method: "PATCH",
    body: JSON.stringify({ action })
  });
}

export function createManualReply(feedbackId: string, reply: string): Promise<Reply> {
  return api.request<Reply>(`/feedback/${feedbackId}/replies`, {
    method: "POST",
    body: JSON.stringify({ reply })
  });
}

export function requestAiDraft(feedbackId: string): Promise<Reply> {
  return api.request<Reply>(`/feedback/${feedbackId}/replies/ai-draft`, { method: "POST" });
}

export function decideReply(replyId: string, decision: ReplyDecision, reply?: string): Promise<Reply> {
  const includeText = decision === "Edit" || decision === "Save" || (decision === "Approve" && reply);
  const body = includeText ? { decision, reply: reply ?? "" } : { decision };
  return api.request<Reply>(`/replies/${replyId}/decision`, {
    method: "PATCH",
    body: JSON.stringify(body)
  });
}

export function listComplaints(query: {
  overdue?: boolean;
  status?: ComplaintStatus | "";
  priority?: ComplaintPriority | "";
}): Promise<ComplaintSummary[]> {
  if (query.overdue) {
    return api.request<ComplaintSummary[]>("/complaints?overdue=true");
  }
  const params = new URLSearchParams();
  if (query.status) {
    params.set("status", query.status);
  }
  if (query.priority) {
    params.set("priority", query.priority);
  }
  const qs = params.toString();
  return api.request<ComplaintSummary[]>(qs ? `/complaints?${qs}` : "/complaints");
}

export function listAssignees(): Promise<StaffAssignee[]> {
  return api.request<StaffAssignee[]>("/complaints/assignees");
}

export function updateComplaintStatus(
  id: string,
  status: ComplaintStatus,
  assignedTo?: string
): Promise<ComplaintSummary> {
  return api.request<ComplaintSummary>(`/complaints/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify(assignedTo ? { status, assignedTo } : { status })
  });
}
