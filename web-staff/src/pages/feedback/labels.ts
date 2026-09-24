import type {
  ComplaintPriority,
  ComplaintStatus,
  FeedbackCategory,
  FeedbackReplyStatus,
  FeedbackSentiment,
  FeedbackStatus,
  FeedbackSummary
} from "../../api/feedback";

export const ANONYMOUS_PATIENT_LABEL = "Anonymous patient";

const CATEGORY_LABELS: Record<FeedbackCategory, string> = {
  TreatmentQuality: "Treatment quality",
  WaitingTime: "Waiting time",
  StaffService: "Staff service",
  FacilityIssue: "Facility issue",
  Other: "Other"
};

const SENTIMENT_LABELS: Record<FeedbackSentiment, string> = {
  Positive: "Positive",
  Neutral: "Neutral",
  Negative: "Negative"
};

const STATUS_LABELS: Record<FeedbackStatus, string> = {
  Visible: "Visible",
  Hidden: "Hidden",
  PendingModeration: "Pending moderation"
};

const REPLY_STATUS_LABELS: Record<FeedbackReplyStatus, string> = {
  Draft: "Draft",
  Approved: "Approved",
  Rejected: "Rejected",
  Posted: "Posted"
};

const COMPLAINT_STATUS_LABELS: Record<ComplaintStatus, string> = {
  Open: "Open",
  InProgress: "In progress",
  Escalated: "Escalated",
  Resolved: "Resolved"
};

const PRIORITY_LABELS: Record<ComplaintPriority, string> = {
  Normal: "Normal",
  High: "High"
};

export const FEEDBACK_STATUSES = Object.keys(STATUS_LABELS) as FeedbackStatus[];
export const FEEDBACK_CATEGORIES = Object.keys(CATEGORY_LABELS) as FeedbackCategory[];
export const FEEDBACK_SENTIMENTS = Object.keys(SENTIMENT_LABELS) as FeedbackSentiment[];
export const COMPLAINT_STATUSES = Object.keys(COMPLAINT_STATUS_LABELS) as ComplaintStatus[];
export const COMPLAINT_PRIORITIES = Object.keys(PRIORITY_LABELS) as ComplaintPriority[];

export function displayPatientName(feedback: { isAnonymous: boolean; patientName: string }): string {
  if (feedback.isAnonymous) {
    return ANONYMOUS_PATIENT_LABEL;
  }
  const name = feedback.patientName.trim();
  return name.length > 0 ? name : "Patient";
}

export function categoryLabel(category: FeedbackCategory | null): string {
  return category ? CATEGORY_LABELS[category] : "Uncategorized";
}

export function sentimentLabel(sentiment: FeedbackSentiment | null): string {
  return sentiment ? SENTIMENT_LABELS[sentiment] : "Unscored";
}

export function feedbackStatusLabel(status: FeedbackStatus): string {
  return STATUS_LABELS[status];
}

export function replyStatusLabel(status: FeedbackReplyStatus): string {
  return REPLY_STATUS_LABELS[status];
}

export function complaintStatusLabel(status: ComplaintStatus): string {
  return COMPLAINT_STATUS_LABELS[status];
}

export function priorityLabel(priority: ComplaintPriority): string {
  return PRIORITY_LABELS[priority];
}

export function commentSnippet(comment: string, max = 110): string {
  const trimmed = comment.trim();
  if (trimmed.length <= max) {
    return trimmed;
  }
  return `${trimmed.slice(0, max).trimEnd()}…`;
}

export function formatWhen(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return date.toLocaleString();
}

export type CategoryCount = {
  category: FeedbackCategory;
  label: string;
  count: number;
};

/** Counts categories on the supplied rows. Null categories are omitted. */
export function categoryCounts(items: FeedbackSummary[]): CategoryCount[] {
  const counts = new Map<FeedbackCategory, number>();
  for (const item of items) {
    if (!item.category) {
      continue;
    }
    counts.set(item.category, (counts.get(item.category) ?? 0) + 1);
  }
  return [...counts.entries()]
    .map(([category, count]) => ({ category, label: CATEGORY_LABELS[category], count }))
    .sort((a, b) => b.count - a.count || a.label.localeCompare(b.label));
}
