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
from app.state_store import get_state_store
from app.tools.tools import (
    FeedbackCategory,
    FeedbackPriority,
    FeedbackSentiment,
    FeedbackSummary,
    HospitalApiError,
    PromptInjectionError,
    analyze_sentiment as _analyze_sentiment,
    categorize as _categorize,
    check_similar_feedback as _check_similar_feedback,
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
            return await fn(*args, **kwargs)

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
    status: str


def _optional_enum(enum_type: type[FeedbackSentiment] | type[FeedbackCategory], raw: str | None):
    if not raw:
        return None
    try:
        return enum_type(raw)
    except ValueError:
        return None


async def analyze_node(state: FeedbackGraphState) -> dict:
    """Classify sentiment and category together, and load hospital context when it is available."""
    text = state["comment_text"]
    sentiment, category, record = await asyncio.gather(
        analyze_sentiment(text),
        categorize(text),
        get_feedback(state["feedback_id"]),
    )
    updates: dict = {
        "sentiment": None if sentiment is None else sentiment.value,
        "category": None if category is None else category.value,
    }
    if record is not None:
        updates["rating"] = record.rating
        updates["is_anonymous"] = record.is_anonymous
    return updates


async def check_similar_node(state: FeedbackGraphState) -> dict:
    category = _optional_enum(FeedbackCategory, state.get("category"))
    if category is None:
        return {"similar_feedback_count": None}
    try:
        count = await check_similar_feedback(state["patient_id"], category)
    except HospitalApiError:
        logger.warning("Similar-feedback lookup failed; continuing without a count.")
        return {"similar_feedback_count": None}
    return {"similar_feedback_count": count}


async def flag_node(state: FeedbackGraphState) -> dict:
    """Deterministic priority. No model call.

    Member 4 frontend (Prompt 4.4): the staff dashboard should poll or display
    immediate_dashboard_alert distinctly for High priority if that surface is
    not already covered. This agent only sets the flag; it does not publish it.
    """
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


async def awaiting_review_node(_state: FeedbackGraphState) -> dict:
    """Terminal state. The caller stores any draft. This agent never posts a reply."""
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
            current = await store.get(workflow_id)
            completed = [*(current.completed_steps if current else []), step]
            tool_results = [*(current.tool_results if current else []), ToolResult(
                tool=step, succeeded=True, output=_jsonable(changes),
            )]
            await store.update(workflow_id, completed_steps=completed, tool_results=tool_results)
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
    )


async def run_feedback_support(request: FeedbackAgentRequest) -> FeedbackAgentResponse:
    workflow_id = str(uuid4())
    store = get_state_store()
    related_id = request.feedback_id
    try:
        UUID(related_id)
    except ValueError:
        related_id = None
    await store.save(
        WorkflowState(
            workflow_id=workflow_id,
            objective=f"Review feedback {request.feedback_id} and leave a draft for staff.",
            plan=list(_PLAN),
            approval_status=ApprovalStatus.PENDING,
            agent_name="feedback_support",
            related_entity_type="Feedback" if related_id else None,
            related_entity_id=related_id,
        )
    )
    reason = injection_reason(request.comment_text)
    if reason:
        logger.warning(
            "Refused feedback %s: prompt injection (%s). No model call.",
            request.feedback_id,
            reason,
        )
        await store.update(workflow_id, errors=[reason], final_outcome=reason)
        return _refused(workflow_id, reason)
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
                "status": "",
            }
        )
    except PromptInjectionError as exc:
        logger.warning(
            "Refused feedback %s: prompt injection (%s). No model call.",
            request.feedback_id,
            exc.reason,
        )
        await store.update(workflow_id, errors=[exc.reason], final_outcome=exc.reason)
        return _refused(workflow_id, exc.reason)
    await store.update(
        workflow_id,
        completed_steps=list(_PLAN),
        final_outcome="awaiting_review",
    )
    return _response(result)
