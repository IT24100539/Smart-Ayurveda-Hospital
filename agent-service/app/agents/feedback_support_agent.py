"""Feedback & support agent.

Decision flow, in order: analyze, check_similar, flag, draft, awaiting_review.
The agent returns the draft and the analysis. It never publishes a reply.
"""

from __future__ import annotations

import asyncio
import functools
import inspect
import logging
from typing import TypedDict
from uuid import UUID, uuid4

from langgraph.graph import END, StateGraph
from pydantic import BaseModel, Field

from app.schemas import ApprovalStatus, ToolResult, WorkflowState
from app.state_store import get_state_store, persist, persistence_warning, reset_persistence_warning
from app.tools.tools import (
    FeedbackCategory,
    FeedbackPriority,
    FeedbackSentiment,
    FeedbackSummary,
    PromptInjectionError,
    _is_timeout,
    analyze_sentiment as _analyze_sentiment,
    reconcile_sentiment,
    categorize as _categorize,
    check_similar_feedback as _check_similar_feedback,
    classify_by_rules,
    draft_reply as _draft_reply,
    flag_priority as _flag_priority,
    get_feedback as _get_feedback,
    injection_reason,
)

logger = logging.getLogger(__name__)

_PLAN = ["analyze", "check_similar", "flag", "draft", "awaiting_review"]

ALLOWED_TOOLS = frozenset(
    {
        "analyze_sentiment",
        "categorize",
        "get_feedback",
        "check_similar_feedback",
        "flag_priority",
        "draft_reply",
    }
)


class DisallowedToolError(RuntimeError):
    """Raised when a node tries to call a tool that is not on ALLOWED_TOOLS."""


def require_allowed_tool(name: str) -> None:
    if name not in ALLOWED_TOOLS:
        raise DisallowedToolError(
            f"Feedback-support agent cannot call {name!r}. "
            f"Allowed tools: {sorted(ALLOWED_TOOLS)}."
        )


def _allowed(fn):
    name = fn.__name__
    require_allowed_tool(name)

    if inspect.iscoroutinefunction(fn):

        @functools.wraps(fn)
        async def async_wrapper(*args, **kwargs):
            require_allowed_tool(name)
            try:
                return await fn(*args, **kwargs)
            except Exception as exc:
                if _is_timeout(exc):
                    logger.exception("Tool %s timed out.", name)
                else:
                    logger.exception("Tool %s failed.", name)
                raise

        return async_wrapper

    @functools.wraps(fn)
    def sync_wrapper(*args, **kwargs):
        require_allowed_tool(name)
        return fn(*args, **kwargs)

    return sync_wrapper


analyze_sentiment = _allowed(_analyze_sentiment)
categorize = _allowed(_categorize)
get_feedback = _allowed(_get_feedback)
check_similar_feedback = _allowed(_check_similar_feedback)
flag_priority = _allowed(_flag_priority)
draft_reply = _allowed(_draft_reply)


class FeedbackAgentRequest(BaseModel):
    feedback_id: str = Field(min_length=1)
    comment_text: str = Field(min_length=1)
    patient_id: str = Field(min_length=1)


class FeedbackAgentResponse(BaseModel):
    sentiment: FeedbackSentiment | None
    category: FeedbackCategory | None
    priority: FeedbackPriority | None
    similar_feedback_count: int | None
    suggested_reply: str | None
    draft_skipped: bool
    workflow_id: str
    status: str = "awaiting_review"
    immediate_dashboard_alert: bool = False
    refusal_reason: str | None = None
    classified_by: str | None = None
    warning: str | None = None


class FeedbackGraphState(TypedDict, total=False):
    workflow_id: str
    feedback_id: str
    comment_text: str
    patient_id: str
    rating: int | None
    is_anonymous: bool
    sentiment: str | None
    category: str | None
    similar_feedback_count: int | None
    priority: str | None
    immediate_dashboard_alert: bool
    suggested_reply: str | None
    draft_skipped: bool
    manual_review_reason: str
    classified_by: str | None
    status: str


def _optional_enum(enum_type: type[FeedbackSentiment] | type[FeedbackCategory], raw: str | None):
    if not raw:
        return None
    try:
        return enum_type(raw)
    except ValueError:
        return None


def _log_analysis_failure(label: str, exc: BaseException) -> str:
    """Log the real traceback. The returned sentence is safe to show to callers."""
    message = f"{label} timed out." if _is_timeout(exc) else f"{label} failed."
    if not isinstance(exc, Exception):
        raise exc
    try:
        raise exc
    except Exception:
        logger.exception(message)
    return message


def _review_reason(current: str | None, extra: str) -> str:
    extra = extra.strip()
    if not current:
        return f"Needs manual review. {extra}"
    if extra in current:
        return current
    return f"{current} {extra}"


async def analyze_node(state: FeedbackGraphState) -> dict:
    """Classify sentiment and category together, and load hospital context when it is available."""
    text = state["comment_text"]
    sentiment_result, category_result, record_result = await asyncio.gather(
        analyze_sentiment(text),
        categorize(text),
        get_feedback(state["feedback_id"]),
        return_exceptions=True,
    )
    reasons: list[str] = []
    sentiment = None if isinstance(sentiment_result, BaseException) else sentiment_result
    category = None if isinstance(category_result, BaseException) else category_result
    record = record_result
    if isinstance(sentiment_result, BaseException):
        reasons.append(_log_analysis_failure("Sentiment analysis", sentiment_result))
    if isinstance(category_result, BaseException):
        reasons.append(_log_analysis_failure("Category analysis", category_result))
    if isinstance(record_result, BaseException):
        reasons.append(_log_analysis_failure("Feedback lookup", record_result))
        record = None

    # None from the tools means the model was unavailable or timed out. Rules fill only that gap.
    model_missing = (
        (sentiment is None and not isinstance(sentiment_result, BaseException))
        or (category is None and not isinstance(category_result, BaseException))
    )
    classified_by = None
    if model_missing:
        rule_sentiment, rule_category = classify_by_rules(text)
        if sentiment is None and not isinstance(sentiment_result, BaseException):
            sentiment = rule_sentiment
        if category is None and not isinstance(category_result, BaseException):
            category = rule_category
        classified_by = "rules"
        logger.info("Classification used keyword rules because the model was unavailable or timed out.")
    elif sentiment is not None and category is not None:
        classified_by = "model"

    rating = None if record is None or isinstance(record, BaseException) else record.rating
    if sentiment is not None:
        sentiment = reconcile_sentiment(text, sentiment, rating)

    if sentiment is None and not isinstance(sentiment_result, BaseException):
        reasons.append("Sentiment analysis failed.")
    if category is None and not isinstance(category_result, BaseException):
        reasons.append("Category analysis failed.")

    updates: dict = {
        "sentiment": None if sentiment is None else sentiment.value,
        "category": None if category is None else category.value,
        "classified_by": classified_by,
    }
    if record is not None:
        updates["rating"] = record.rating
        updates["is_anonymous"] = record.is_anonymous
    if reasons:
        updates["manual_review_reason"] = _review_reason(state.get("manual_review_reason"), " ".join(reasons))
    return updates


async def check_similar_node(state: FeedbackGraphState) -> dict:
    category = _optional_enum(FeedbackCategory, state.get("category"))
    if category is None:
        return {"similar_feedback_count": None}
    try:
        count = await check_similar_feedback(state["patient_id"], category)
    except Exception as exc:
        detail = _log_analysis_failure("Similar-feedback lookup", exc)
        return {
            "similar_feedback_count": None,
            "manual_review_reason": _review_reason(state.get("manual_review_reason"), detail),
        }
    return {"similar_feedback_count": count}


async def flag_node(state: FeedbackGraphState) -> dict:
    """Deterministic priority. No model call.

    Member 4 frontend (Prompt 4.4): the staff dashboard should poll or display
    immediate_dashboard_alert distinctly for High priority if that surface is
    not already covered. This agent only sets the flag; it does not publish it.
    """
    if state.get("manual_review_reason"):
        return {"priority": FeedbackPriority.HIGH.value, "immediate_dashboard_alert": True}

    sentiment = _optional_enum(FeedbackSentiment, state.get("sentiment"))
    category = _optional_enum(FeedbackCategory, state.get("category"))
    count = state.get("similar_feedback_count")
    if sentiment is None and category is None and count is None:
        return {"priority": None, "immediate_dashboard_alert": False}

    summary = _summary(state, sentiment, category, 0 if count is None else count)
    flagged = flag_priority(summary)
    return {
        "priority": flagged.priority.value,
        "immediate_dashboard_alert": flagged.immediate_dashboard_alert,
    }


async def draft_node(state: FeedbackGraphState) -> dict:
    """Optional reply. A model failure skips the draft and leaves the earlier results in place."""
    if state.get("manual_review_reason"):
        return {"suggested_reply": None, "draft_skipped": True}

    sentiment = _optional_enum(FeedbackSentiment, state.get("sentiment"))
    category = _optional_enum(FeedbackCategory, state.get("category"))
    if sentiment is None or category is None:
        return {"suggested_reply": None, "draft_skipped": True}

    count = state.get("similar_feedback_count")
    summary = _summary(state, sentiment, category, 0 if count is None else count)
    reply = await draft_reply(summary, sentiment, category)
    if not reply:
        return {"suggested_reply": None, "draft_skipped": True}
    return {"suggested_reply": reply, "draft_skipped": False}


async def awaiting_review_node(state: FeedbackGraphState) -> dict:
    """Terminal state. The caller stores any draft. This agent never posts a reply."""
    if state.get("manual_review_reason"):
        return {"status": "needs_manual_review"}
    return {"status": "awaiting_review"}


def _summary(
    state: FeedbackGraphState,
    sentiment: FeedbackSentiment | None,
    category: FeedbackCategory | None,
    similar_feedback_count: int,
) -> FeedbackSummary:
    return FeedbackSummary(
        feedback_id=state["feedback_id"],
        comment=state["comment_text"],
        rating=state.get("rating"),
        patient_id=state.get("patient_id"),
        is_anonymous=bool(state.get("is_anonymous")),
        sentiment=sentiment,
        category=category,
        similar_feedback_count=similar_feedback_count,
    )


def _jsonable(changes: dict) -> dict:
    output: dict = {}
    for key, value in changes.items():
        if isinstance(value, (str, int, float, bool)):
            output[key] = value
        elif value is not None:
            output[key] = str(value)
    return output


def _tracked(step: str, node):
    async def transition(state: FeedbackGraphState) -> dict:
        changes = await node(state)
        workflow_id = state.get("workflow_id")
        if workflow_id:
            store = get_state_store()
            current = await persist(store.get(workflow_id))
            completed = [*(current.completed_steps if current else []), step]
            tool_results = [*(current.tool_results if current else []), ToolResult(
                tool=step, succeeded=True, output=_jsonable(changes),
            )]
            await persist(store.update(workflow_id, completed_steps=completed, tool_results=tool_results))
        return changes

    return transition


def build_feedback_support_graph():
    graph = StateGraph(FeedbackGraphState)
    graph.add_node("analyze", _tracked("analyze", analyze_node))
    graph.add_node("check_similar", _tracked("check_similar", check_similar_node))
    graph.add_node("flag", _tracked("flag", flag_node))
    graph.add_node("draft", _tracked("draft", draft_node))
    graph.add_node("awaiting_review", _tracked("awaiting_review", awaiting_review_node))
    graph.set_entry_point("analyze")
    graph.add_edge("analyze", "check_similar")
    graph.add_edge("check_similar", "flag")
    graph.add_edge("flag", "draft")
    graph.add_edge("draft", "awaiting_review")
    graph.add_edge("awaiting_review", END)
    return graph.compile()


_GRAPH = build_feedback_support_graph()


def _refused(workflow_id: str, reason: str) -> FeedbackAgentResponse:
    """Injection refusal. Classification and the draft are not attempted."""
    return FeedbackAgentResponse(
        sentiment=None,
        category=None,
        priority=None,
        similar_feedback_count=None,
        suggested_reply=None,
        draft_skipped=True,
        workflow_id=workflow_id,
        status="awaiting_review",
        immediate_dashboard_alert=False,
        refusal_reason=reason,
        classified_by=None,
    )


def _response(state: FeedbackGraphState) -> FeedbackAgentResponse:
    sentiment = _optional_enum(FeedbackSentiment, state.get("sentiment"))
    category = _optional_enum(FeedbackCategory, state.get("category"))
    priority_raw = state.get("priority")
    priority = FeedbackPriority(priority_raw) if priority_raw else None
    return FeedbackAgentResponse(
        sentiment=sentiment,
        category=category,
        priority=priority,
        similar_feedback_count=state.get("similar_feedback_count"),
        suggested_reply=state.get("suggested_reply"),
        draft_skipped=bool(state.get("draft_skipped")),
        workflow_id=state["workflow_id"],
        status=state.get("status") or "awaiting_review",
        immediate_dashboard_alert=bool(state.get("immediate_dashboard_alert")),
        refusal_reason=state.get("manual_review_reason"),
        classified_by=state.get("classified_by"),
    )


def _with_warning(response: FeedbackAgentResponse) -> FeedbackAgentResponse:
    warning = persistence_warning()
    if warning:
        response.warning = warning
    return response


async def run_feedback_support(request: FeedbackAgentRequest) -> FeedbackAgentResponse:
    reset_persistence_warning()
    workflow_id = str(uuid4())
    store = get_state_store()
    related_id = request.feedback_id
    try:
        UUID(related_id)
    except ValueError:
        related_id = None
    await persist(store.save(
        WorkflowState(
            workflow_id=workflow_id,
            objective=f"Review feedback {request.feedback_id} and leave a draft for staff.",
            plan=list(_PLAN),
            approval_status=ApprovalStatus.PENDING,
            agent_name="feedback_support",
            related_entity_type="Feedback" if related_id else None,
            related_entity_id=related_id,
        )
    ))
    reason = injection_reason(request.comment_text)
    if reason:
        logger.warning(
            "Refused feedback %s: prompt injection (%s). No model call.",
            request.feedback_id,
            reason,
        )
        await persist(store.update(
            workflow_id,
            errors=[reason],
            final_outcome=reason,
            approval_status=None,
        ))
        return _with_warning(_refused(workflow_id, reason))
    try:
        result = await _GRAPH.ainvoke(
            {
                "workflow_id": workflow_id,
                "feedback_id": request.feedback_id,
                "comment_text": request.comment_text.strip(),
                "patient_id": request.patient_id,
                "rating": None,
                "is_anonymous": False,
                "sentiment": None,
                "category": None,
                "similar_feedback_count": None,
                "priority": None,
                "immediate_dashboard_alert": False,
                "suggested_reply": None,
                "draft_skipped": False,
                "classified_by": None,
                "status": "",
            }
        )
    except PromptInjectionError as exc:
        logger.warning(
            "Refused feedback %s: prompt injection (%s). No model call.",
            request.feedback_id,
            exc.reason,
        )
        await persist(store.update(
            workflow_id,
            errors=[exc.reason],
            final_outcome=exc.reason,
            approval_status=None,
        ))
        return _with_warning(_refused(workflow_id, exc.reason))
    reason = result.get("manual_review_reason")
    draft_text = (result.get("suggested_reply") or "").strip()
    draft_skipped = bool(result.get("draft_skipped")) or not draft_text
    await persist(store.update(
        workflow_id,
        completed_steps=list(_PLAN),
        errors=[reason] if reason else [],
        final_outcome="needs_manual_review" if reason else "awaiting_review",
        approval_status=None if draft_skipped else ApprovalStatus.PENDING,
    ))
    return _with_warning(_response(result))
