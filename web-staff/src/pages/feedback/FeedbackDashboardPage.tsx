import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  createManualReply,
  decideReply,
  errorMessage,
  getFeedbackStats,
  getStaffFeedback,
  listStaffAlerts,
  listStaffFeedback,
  moderateFeedback,
  requestAiDraft,
  type FeedbackCategory,
  type FeedbackDetail,
  type FeedbackSentiment,
  type FeedbackStats,
  type FeedbackStatus,
  type FeedbackSummary,
  type ModerationAction,
  type PagedResult,
  type Reply,
  type ReplyDecision,
  type StaffAlert
} from "../../api/feedback";
import {
  categoryCounts,
  categoryLabel,
  commentSnippet,
  displayPatientName,
  FEEDBACK_CATEGORIES,
  FEEDBACK_SENTIMENTS,
  FEEDBACK_STATUSES,
  feedbackStatusLabel,
  formatWhen,
  replyStatusLabel,
  sentimentLabel
} from "./labels";

const PAGE_SIZE = 10;

const fieldClass =
  "mt-1 w-full rounded-lg border border-surface-border bg-white px-3 py-2 text-sm outline-none ring-primary focus:ring-2";
const secondaryButton =
  "rounded-lg border border-surface-border bg-white px-3 py-1.5 text-sm font-medium text-primary hover:bg-primary-muted disabled:opacity-60";
const primaryButton =
  "rounded-lg bg-primary px-3 py-1.5 text-sm font-semibold text-white hover:bg-primary-dark disabled:opacity-60";

type Filters = {
  status: FeedbackStatus | "";
  rating: "" | "1" | "2" | "3" | "4" | "5";
  category: FeedbackCategory | "";
  sentiment: FeedbackSentiment | "";
  sort: string;
};

const EMPTY_FILTERS: Filters = {
  status: "",
  rating: "",
  category: "",
  sentiment: "",
  sort: ""
};

type RowExtra = {
  replies: Reply[];
  moderatedBy: string | null;
  moderatedAt: string | null;
  detailState: "loading" | "ready" | "error";
  detailError?: string;
};

type BusyKind = "generate" | "decide" | "moderate" | "manual";

function findDraft(replies: Reply[]): Reply | undefined {
  return [...replies]
    .filter((reply) => reply.isAiGenerated && reply.status === "Draft")
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
    .at(-1);
}

function sortReplies(replies: Reply[]): Reply[] {
  return [...replies].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

function moderationNote(extra: RowExtra | undefined): string | null {
  if (!extra?.moderatedBy || !extra.moderatedAt) {
    return null;
  }
  return `Moderated by ${extra.moderatedBy} at ${formatWhen(extra.moderatedAt)}`;
}

export function FeedbackDashboardPage() {
  const [filters, setFilters] = useState<Filters>(EMPTY_FILTERS);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [reloadKey, setReloadKey] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<PagedResult<FeedbackSummary> | null>(null);
  const [stats, setStats] = useState<FeedbackStats | null>(null);
  const [alerts, setAlerts] = useState<StaffAlert[]>([]);
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [extras, setExtras] = useState<Record<string, RowExtra>>({});
  const [busy, setBusy] = useState<{ id: string; kind: BusyKind } | null>(null);
  const [rowError, setRowError] = useState<{ id: string; message: string } | null>(null);

  const listGeneration = useRef(0);
  const expandedRef = useRef<string | null>(null);
  const detailRequests = useRef(new Set<string>());
  const extrasRef = useRef(extras);
  extrasRef.current = extras;

  useEffect(() => {
    const generation = ++listGeneration.current;
    detailRequests.current.clear();
    expandedRef.current = null;
    setExpandedId(null);
    setExtras({});
    setRowError(null);
    setLoading(true);
    setError(null);
    setResult(null);

    listStaffFeedback({
      status: filters.status,
      rating: filters.rating === "" ? "" : Number(filters.rating),
      category: filters.category,
      sentiment: filters.sentiment,
      sort: filters.sort,
      search,
      page,
      pageSize: PAGE_SIZE
    })
      .then((pageResult) => {
        if (generation !== listGeneration.current) {
          return;
        }
        setResult(pageResult);
        setError(null);
      })
      .catch((err: unknown) => {
        if (generation !== listGeneration.current) {
          return;
        }
        setResult(null);
        setError(errorMessage(err, "Unable to load feedback."));
      })
      .finally(() => {
        if (generation === listGeneration.current) {
          setLoading(false);
        }
      });
  }, [filters, page, reloadKey, search]);

  useEffect(() => {
    let cancelled = false;
    getFeedbackStats()
      .then((next) => {
        if (!cancelled) {
          setStats(next);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setStats(null);
        }
      });
    listStaffAlerts()
      .then((next) => {
        if (!cancelled) {
          setAlerts(next);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setAlerts([]);
        }
      });
    return () => {
      cancelled = true;
    };
  }, [reloadKey]);

  const visibleItems = result?.items ?? [];

  const themes = useMemo(() => categoryCounts(result?.items ?? []), [result]);
  const pageCount = Math.max(1, Math.ceil((result?.totalCount ?? 0) / PAGE_SIZE));
  const filtersActive =
    filters.status !== "" ||
    filters.rating !== "" ||
    filters.category !== "" ||
    filters.sentiment !== "" ||
    search.trim() !== "";

  function setFilter<K extends keyof Filters>(key: K, value: Filters[K]) {
    setFilters((current) => ({ ...current, [key]: value }));
    setPage(1);
  }

  function applyDetail(detail: FeedbackDetail) {
    detailRequests.current.add(detail.id);
    setExtras((current) => ({
      ...current,
      [detail.id]: {
        replies: detail.replies,
        moderatedBy: detail.moderatedBy,
        moderatedAt: detail.moderatedAt,
        detailState: "ready"
      }
    }));
    setResult((current) => {
      if (!current) {
        return current;
      }
      return {
        ...current,
        items: current.items.map((item) =>
          item.id === detail.id
            ? {
                ...item,
                status: detail.status,
                comment: detail.comment,
                rating: detail.rating,
                sentiment: detail.sentiment,
                category: detail.category,
                isAnonymous: detail.isAnonymous,
                patientName: detail.patientName,
                patientId: detail.patientId,
                likeCount: detail.likeCount,
                dislikeCount: detail.dislikeCount,
                postedReplyCount: detail.replies.filter((reply) => reply.status === "Posted").length
              }
            : item
        )
      };
    });
  }

  function upsertReply(id: string, reply: Reply) {
    setExtras((current) => {
      const existing = current[id] ?? {
        replies: [],
        moderatedBy: null,
        moderatedAt: null,
        detailState: "ready" as const
      };
      const replies = existing.replies.some((item) => item.id === reply.id)
        ? existing.replies.map((item) => (item.id === reply.id ? reply : item))
        : [...existing.replies, reply];
      return {
        ...current,
        [id]: { ...existing, replies, detailState: "ready" }
      };
    });
  }

  function beginDetailLoad(id: string) {
    if (detailRequests.current.has(id)) {
      return;
    }
    detailRequests.current.add(id);
    setExtras((current) => ({
      ...current,
      [id]: {
        replies: current[id]?.replies ?? [],
        moderatedBy: current[id]?.moderatedBy ?? null,
        moderatedAt: current[id]?.moderatedAt ?? null,
        detailState: "loading"
      }
    }));
    void loadDetail(id);
  }

  async function loadDetail(id: string) {
    const generation = listGeneration.current;
    try {
      const detail = await getStaffFeedback(id);
      if (generation !== listGeneration.current) {
        return;
      }
      applyDetail(detail);
    } catch (err: unknown) {
      if (generation !== listGeneration.current) {
        return;
      }
      detailRequests.current.delete(id);
      setExtras((current) => ({
        ...current,
        [id]: {
          replies: current[id]?.replies ?? [],
          moderatedBy: current[id]?.moderatedBy ?? null,
          moderatedAt: current[id]?.moderatedAt ?? null,
          detailState: "error",
          detailError: errorMessage(err, "Unable to load replies.")
        }
      }));
    }
  }

  function toggleRow(id: string) {
    const willCollapse = expandedRef.current === id;
    expandedRef.current = willCollapse ? null : id;
    setExpandedId(expandedRef.current);
    if (!willCollapse) {
      beginDetailLoad(id);
    }
  }

  async function onModerate(id: string, action: ModerationAction) {
    const generation = listGeneration.current;
    setBusy({ id, kind: "moderate" });
    setRowError(null);
    try {
      const detail = await moderateFeedback(id, action);
      if (generation !== listGeneration.current) {
        return;
      }
      applyDetail(detail);
    } catch (err: unknown) {
      setRowError({ id, message: errorMessage(err, "Unable to update moderation.") });
    } finally {
      setBusy((current) => (current?.id === id ? null : current));
    }
  }

  async function onGenerate(id: string) {
    const generation = listGeneration.current;
    setBusy({ id, kind: "generate" });
    setRowError(null);
    try {
      const reply = await requestAiDraft(id);
      if (generation !== listGeneration.current) {
        return;
      }
      upsertReply(id, reply);
    } catch (err: unknown) {
      setRowError({ id, message: errorMessage(err, "Unable to generate a reply.") });
    } finally {
      setBusy((current) => (current?.id === id ? null : current));
    }
  }

  async function onDecide(id: string, decision: ReplyDecision, replyText?: string) {
    const draft = findDraft(extrasRef.current[id]?.replies ?? []);
    if (!draft) {
      setRowError({ id, message: "Generate a reply before reviewing it." });
      return;
    }
    const generation = listGeneration.current;
    setBusy({ id, kind: "decide" });
    setRowError(null);
    try {
      const updated = await decideReply(draft.id, decision, replyText);
      if (generation !== listGeneration.current) {
        return;
      }
      upsertReply(id, updated);
    } catch (err: unknown) {
      setRowError({ id, message: errorMessage(err, "Unable to update the draft.") });
    } finally {
      setBusy((current) => (current?.id === id ? null : current));
    }
  }

  async function onManual(id: string, reply: string) {
    const generation = listGeneration.current;
    setBusy({ id, kind: "manual" });
    setRowError(null);
    try {
      const created = await createManualReply(id, reply);
      if (generation !== listGeneration.current) {
        return;
      }
      upsertReply(id, created);
    } catch (err: unknown) {
      setRowError({ id, message: errorMessage(err, "Unable to post the reply.") });
      throw err;
    } finally {
      setBusy((current) => (current?.id === id ? null : current));
    }
  }

  const emptyCopy = filtersActive ? "No feedback matches these filters." : "No patient feedback yet.";

  return (
    <div>
      {/* Stretch goal: a weekly digest produced by the feedback agent from the full
          week of comments. This card only counts categories on the current page. */}
      {alerts.length > 0 ? (
        <section aria-label="Feedback alerts" className="mb-4 space-y-2">
          {alerts.map((alert) => (
            <p key={alert.id} className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-danger" role="status">
              <span className="font-semibold">{alert.title}. </span>
              {alert.message}
            </p>
          ))}
        </section>
      ) : null}

      {stats ? (
        <dl className="mb-4 grid gap-3 sm:grid-cols-4">
          <div className="rounded-xl border border-surface-border bg-surface-raised px-4 py-3">
            <dt className="text-xs font-semibold uppercase tracking-wide text-muted">Comments</dt>
            <dd className="mt-1 font-serif text-2xl text-primary-dark">{stats.total}</dd>
          </div>
          <div className="rounded-xl border border-surface-border bg-surface-raised px-4 py-3">
            <dt className="text-xs font-semibold uppercase tracking-wide text-muted">Average rating</dt>
            <dd className="mt-1 font-serif text-2xl text-primary-dark">{stats.averageRating.toFixed(1)}</dd>
          </div>
          <div className="rounded-xl border border-surface-border bg-surface-raised px-4 py-3">
            <dt className="text-xs font-semibold uppercase tracking-wide text-muted">Awaiting review</dt>
            <dd className="mt-1 font-serif text-2xl text-primary-dark">{stats.pendingModeration}</dd>
          </div>
          <div className="rounded-xl border border-surface-border bg-surface-raised px-4 py-3">
            <dt className="text-xs font-semibold uppercase tracking-wide text-muted">Negative</dt>
            <dd className="mt-1 font-serif text-2xl text-primary-dark">{stats.negative}</dd>
          </div>
        </dl>
      ) : null}

      <section
        aria-labelledby="weekly-digest-heading"
        className="rounded-2xl border border-surface-border bg-surface-raised p-4"
      >
        <h2 id="weekly-digest-heading" className="font-serif text-lg font-semibold text-primary-dark">
          Weekly digest
        </h2>
        <p className="mt-1 text-sm text-muted">Recurring themes from the feedback on this page.</p>
        {loading ? <p className="mt-3 text-sm text-muted">Counting themes on this page…</p> : null}
        {!loading && !error && themes.length === 0 ? (
          <p className="mt-3 text-sm text-muted">No categorized themes on this page yet.</p>
        ) : null}
        {!loading && error ? (
          <p className="mt-3 text-sm text-muted">Themes appear after feedback loads.</p>
        ) : null}
        {themes.length > 0 ? (
          <ul className="mt-3 flex flex-wrap gap-2">
            {themes.map((theme) => (
              <li key={theme.category} className="rounded-lg bg-surface px-3 py-2 text-sm">
                <span className="font-semibold text-ink">{theme.label}</span>
                <span className="ml-2 text-muted">{theme.count}</span>
              </li>
            ))}
          </ul>
        ) : null}
      </section>

      <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <label className="block text-sm font-semibold" htmlFor="feedback-status">
          Status
          <select
            id="feedback-status"
            className={fieldClass}
            value={filters.status}
            onChange={(event) => setFilter("status", event.target.value as Filters["status"])}
          >
            <option value="">Any status</option>
            {FEEDBACK_STATUSES.map((status) => (
              <option key={status} value={status}>
                {feedbackStatusLabel(status)}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-semibold" htmlFor="feedback-rating">
          Rating
          <select
            id="feedback-rating"
            className={fieldClass}
            value={filters.rating}
            onChange={(event) => setFilter("rating", event.target.value as Filters["rating"])}
          >
            <option value="">Any rating</option>
            {["1", "2", "3", "4", "5"].map((rating) => (
              <option key={rating} value={rating}>
                {rating} / 5
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-semibold" htmlFor="feedback-category">
          Category
          <select
            id="feedback-category"
            className={fieldClass}
            value={filters.category}
            onChange={(event) => setFilter("category", event.target.value as Filters["category"])}
          >
            <option value="">Any category</option>
            {FEEDBACK_CATEGORIES.map((category) => (
              <option key={category} value={category}>
                {categoryLabel(category)}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-semibold" htmlFor="feedback-sentiment">
          Sentiment
          <select
            id="feedback-sentiment"
            className={fieldClass}
            value={filters.sentiment}
            onChange={(event) => setFilter("sentiment", event.target.value as Filters["sentiment"])}
          >
            <option value="">Any sentiment</option>
            {FEEDBACK_SENTIMENTS.map((sentiment) => (
              <option key={sentiment} value={sentiment}>
                {sentimentLabel(sentiment)}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm font-semibold" htmlFor="feedback-sort">
          Sort
          <select
            id="feedback-sort"
            className={fieldClass}
            value={filters.sort}
            onChange={(event) => setFilter("sort", event.target.value)}
          >
            <option value="">Newest</option>
            <option value="createdat">Oldest</option>
            <option value="ratingdesc">Highest rating</option>
            <option value="rating">Lowest rating</option>
            <option value="sentiment">Sentiment, positive first</option>
            <option value="sentimentdesc">Sentiment, negative first</option>
          </select>
        </label>
        <label className="block text-sm font-semibold" htmlFor="feedback-search">
          Search
          <input
            id="feedback-search"
            className={fieldClass}
            value={search}
            placeholder="Comment or patient name"
            onChange={(event) => {
              setSearch(event.target.value);
              setPage(1);
            }}
          />
        </label>
      </div>

      {loading ? (
        <p className="mt-6 text-sm text-muted" role="status">
          Loading feedback…
        </p>
      ) : null}

      {error ? (
        <div className="mt-6 rounded-xl border border-danger/30 bg-white p-4" role="alert">
          <p className="text-sm text-danger">{error}</p>
          <button type="button" className={`${secondaryButton} mt-3`} onClick={() => setReloadKey((key) => key + 1)}>
            Try again
          </button>
        </div>
      ) : null}

      {!loading && !error && visibleItems.length === 0 ? (
        <p className="mt-6 rounded-xl border border-dashed border-surface-border bg-surface-raised p-6 text-sm text-muted" role="status">
          {emptyCopy}
        </p>
      ) : null}

      {!loading && !error && visibleItems.length > 0 ? (
        <div className="mt-6 overflow-x-auto rounded-2xl border border-surface-border bg-surface-raised">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-surface-border text-xs uppercase tracking-wide text-muted">
              <tr>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Patient
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Rating
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Sentiment
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Category
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Comment
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  Status
                </th>
                <th className="px-4 py-3 font-semibold" scope="col">
                  <span className="sr-only">Open</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {visibleItems.map((item) => (
                <FeedbackRow
                  key={item.id}
                  item={item}
                  extra={extras[item.id]}
                  expanded={expandedId === item.id}
                  busy={busy?.id === item.id ? busy.kind : null}
                  actionError={rowError?.id === item.id ? rowError.message : null}
                  onToggle={() => toggleRow(item.id)}
                  onRetryDetail={() => beginDetailLoad(item.id)}
                  onModerate={(action) => onModerate(item.id, action)}
                  onGenerate={() => onGenerate(item.id)}
                  onDecide={(decision, replyText) => onDecide(item.id, decision, replyText)}
                  onManual={(reply) => onManual(item.id, reply)}
                />
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {result && result.totalCount > 0 ? (
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <p className="text-sm text-muted">{result.totalCount} comments</p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              className={secondaryButton}
              disabled={page <= 1 || loading}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              Previous
            </button>
            <span className="text-sm text-ink">
              Page {page} of {pageCount}
            </span>
            <button
              type="button"
              className={secondaryButton}
              disabled={page >= pageCount || loading}
              onClick={() => setPage((current) => current + 1)}
            >
              Next
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function sentimentTone(sentiment: FeedbackSentiment | null): string {
  if (sentiment === "Positive") {
    return "bg-primary-muted text-primary-dark";
  }
  if (sentiment === "Negative") {
    return "bg-red-100 text-danger";
  }
  if (sentiment === "Neutral") {
    return "bg-surface text-ink";
  }
  return "bg-surface text-muted";
}

function statusTone(status: FeedbackStatus): string {
  if (status === "Visible") {
    return "bg-primary-muted text-primary-dark";
  }
  if (status === "Hidden") {
    return "bg-surface text-muted";
  }
  return "bg-amber-100 text-amber-950";
}

function AiBadge() {
  return (
    <span className="rounded-full bg-primary-muted px-2 py-0.5 text-xs font-semibold uppercase tracking-wide text-primary-dark">
      AI
    </span>
  );
}

type FeedbackRowProps = {
  item: FeedbackSummary;
  extra: RowExtra | undefined;
  expanded: boolean;
  busy: BusyKind | null;
  actionError: string | null;
  onToggle: () => void;
  onRetryDetail: () => void;
  onModerate: (action: ModerationAction) => Promise<void>;
  onGenerate: () => Promise<void>;
  onDecide: (decision: ReplyDecision, replyText?: string) => Promise<void>;
  onManual: (reply: string) => Promise<void>;
};

function FeedbackRow({
  item,
  extra,
  expanded,
  busy,
  actionError,
  onToggle,
  onRetryDetail,
  onModerate,
  onGenerate,
  onDecide,
  onManual
}: FeedbackRowProps) {
  const name = displayPatientName(item);
  const panelId = `feedback-panel-${item.id}`;
  const draft =
    extra && (extra.detailState === "ready" || extra.detailState === "error")
      ? findDraft(extra.replies)
      : undefined;
  const [draftText, setDraftText] = useState("");
  const [manualOpen, setManualOpen] = useState(false);
  const [manualText, setManualText] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const note = moderationNote(extra);

  useEffect(() => {
    setDraftText(draft?.reply ?? "");
  }, [draft?.id, draft?.reply]);

  const history = sortReplies((extra?.replies ?? []).filter((reply) => reply.id !== draft?.id));
  const showActions = extra?.detailState === "ready" || extra?.detailState === "error";
  const generating = busy === "generate";
  const deciding = busy === "decide";

  async function submitManual(event: FormEvent) {
    event.preventDefault();
    if (!manualText.trim()) {
      setFormError("Write a reply before posting.");
      return;
    }
    setFormError(null);
    try {
      await onManual(manualText.trim());
      setManualText("");
      setManualOpen(false);
    } catch {
      // The page surfaces the request error.
    }
  }

  function submitEdit() {
    if (!draftText.trim()) {
      setFormError("Write the reply before posting the edit.");
      return;
    }
    setFormError(null);
    void onDecide("Save", draftText.trim());
  }

  return (
    <>
      <tr className="border-b border-surface-border align-top">
        <td className="px-4 py-3 font-medium text-ink">{name}</td>
        <td className="px-4 py-3 text-ink">
          <span aria-label={`Rating ${item.rating} out of 5`}>{item.rating} / 5</span>
        </td>
        <td className="px-4 py-3">
          <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${sentimentTone(item.sentiment)}`}>
            {sentimentLabel(item.sentiment)}
          </span>
        </td>
        <td className="px-4 py-3">
          <span className="inline-flex rounded-md border border-surface-border bg-surface px-2 py-0.5 text-xs font-medium text-ink">
            {categoryLabel(item.category)}
          </span>
        </td>
        <td className="max-w-xs px-4 py-3 text-ink">{commentSnippet(item.comment)}</td>
        <td className="px-4 py-3">
          <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${statusTone(item.status)}`}>
            {feedbackStatusLabel(item.status)}
          </span>
        </td>
        <td className="px-4 py-3">
          <button
            type="button"
            className={secondaryButton}
            aria-expanded={expanded}
            aria-controls={panelId}
            onClick={onToggle}
          >
            {expanded ? "Collapse" : "Expand"}
            <span className="sr-only"> feedback from {name}</span>
          </button>
        </td>
      </tr>
      {expanded ? (
        <tr className="border-b border-surface-border bg-surface/60">
          <td id={panelId} className="px-4 py-4" colSpan={7}>
            <p className="whitespace-pre-wrap text-sm text-ink">{item.comment}</p>
            <div className="mt-3">
              <span className="group relative inline-flex">
                <button
                  type="button"
                  className={secondaryButton}
                  title={note ?? "No moderator recorded yet"}
                  disabled={busy === "moderate"}
                  onClick={() => void onModerate(item.status === "Hidden" ? "Show" : "Hide")}
                >
                  {busy === "moderate" ? "Updating…" : item.status === "Hidden" ? "Show" : "Hide"}
                </button>
                {note ? (
                  <span
                    role="tooltip"
                    className="invisible absolute left-0 top-full z-10 mt-1 w-72 rounded-md bg-primary-dark px-2 py-1 text-xs font-normal text-white group-hover:visible"
                  >
                    {note}
                  </span>
                ) : null}
              </span>
            </div>

            <div className="mt-4 space-y-3">
              {extra?.detailState === "loading" ? (
                <p className="text-sm text-muted" role="status">
                  Loading replies…
                </p>
              ) : null}
              {extra?.detailState === "error" ? (
                <div role="alert">
                  <p className="text-sm text-danger">{extra.detailError}</p>
                  <button type="button" className={`${secondaryButton} mt-2`} onClick={onRetryDetail}>
                    Try again
                  </button>
                </div>
              ) : null}
              {showActions && history.length === 0 && !draft ? (
                <p className="text-sm text-muted">No replies yet.</p>
              ) : null}
              {showActions && history.length > 0 ? (
                <ul className="space-y-2" aria-label="Replies">
                  {history.map((reply) => (
                    <li key={reply.id} className="rounded-lg border border-surface-border bg-white px-3 py-2">
                      <div className="flex flex-wrap items-center gap-2">
                        {reply.isAiGenerated ? <AiBadge /> : <span className="text-xs font-semibold text-muted">Staff</span>}
                        <span className="text-xs text-muted">{replyStatusLabel(reply.status)}</span>
                      </div>
                      <p className="mt-1 whitespace-pre-wrap text-sm text-ink">{reply.reply}</p>
                    </li>
                  ))}
                </ul>
              ) : null}

              {showActions && draft ? (
                <div className="rounded-lg border border-primary/20 bg-white p-3">
                  <div className="flex items-center gap-2">
                    <h3 className="text-sm font-semibold text-ink">AI draft</h3>
                    {draft.isAiGenerated ? <AiBadge /> : null}
                  </div>
                  <label className="mt-2 block text-sm font-semibold" htmlFor={`draft-${item.id}`}>
                    Draft reply
                    <textarea
                      id={`draft-${item.id}`}
                      className={`${fieldClass} min-h-24`}
                      value={draftText}
                      onChange={(event) => setDraftText(event.target.value)}
                    />
                  </label>
                </div>
              ) : null}

              {showActions ? (
                <div className="flex flex-wrap gap-2">
                  <button type="button" className={secondaryButton} onClick={() => setManualOpen((open) => !open)}>
                    Reply manually
                  </button>
                  <button
                    type="button"
                    className={secondaryButton}
                    disabled={generating || deciding}
                    aria-busy={generating}
                    onClick={() => void onGenerate()}
                  >
                    {generating ? "Generating reply…" : "Generate Reply"}
                  </button>
                  {draft ? (
                    <>
                      <button type="button" className={secondaryButton} disabled={deciding || generating} onClick={submitEdit}>
                        Save edit
                      </button>
                      <button
                        type="button"
                        className={primaryButton}
                        disabled={deciding || generating}
                        onClick={() => {
                          const trimmed = draftText.trim();
                          void onDecide("Approve", trimmed && trimmed !== draft.reply ? trimmed : undefined);
                        }}
                      >
                        Approve & Post
                      </button>
                      <button
                        type="button"
                        className="rounded-lg border border-danger/40 px-3 py-1.5 text-sm font-medium text-danger hover:bg-red-50 disabled:opacity-60"
                        disabled={deciding || generating}
                        onClick={() => void onDecide("Reject")}
                      >
                        Reject
                      </button>
                    </>
                  ) : null}
                </div>
              ) : null}

              {showActions && manualOpen ? (
                <form onSubmit={submitManual}>
                  <label className="block text-sm font-semibold" htmlFor={`manual-${item.id}`}>
                    Staff reply
                    <textarea
                      id={`manual-${item.id}`}
                      className={`${fieldClass} min-h-24`}
                      value={manualText}
                      onChange={(event) => setManualText(event.target.value)}
                    />
                  </label>
                  <button type="submit" className={`${primaryButton} mt-2`} disabled={busy === "manual"}>
                    {busy === "manual" ? "Posting…" : "Post reply"}
                  </button>
                </form>
              ) : null}

              {formError ? (
                <p className="text-sm text-danger" role="alert">
                  {formError}
                </p>
              ) : null}
              {actionError ? (
                <p className="text-sm text-danger" role="alert">
                  {actionError}
                </p>
              ) : null}
            </div>
          </td>
        </tr>
      ) : null}
    </>
  );
}
