"""Coordinator intake delegates to the existing specialist entrypoints."""

from datetime import UTC, datetime, timedelta
from unittest.mock import AsyncMock
from uuid import uuid4

from app.agents.feedback_support_agent import FeedbackAgentResponse
from app.graph.coordinator import CoordinatorRequest, Specialist, coordinate
from app.schemas import SchedulingAgentResponse, ValidationResult


def _ollama(monkeypatch, label: str) -> None:
    class Response:
        def raise_for_status(self) -> None:
            return None

        def json(self) -> dict:
            return {"message": {"content": f'{{"agent": "{label}"}}'}}

    class Client:
        def __init__(self, *args, **kwargs) -> None:
            return None

        async def __aenter__(self):
            return self

        async def __aexit__(self, *args) -> bool:
            return False

        async def post(self, url, json):
            assert "/api/chat" in url
            assert json["messages"][-1]["content"]
            return Response()

    monkeypatch.setattr("app.graph.coordinator.httpx.AsyncClient", Client)


async def test_intake_routes_scheduling_objective_to_scheduling_bed(monkeypatch):
    _ollama(monkeypatch, "scheduling_bed")
    scheduling = AsyncMock(return_value=SchedulingAgentResponse(
        workflow_id=str(uuid4()),
        validation_result=ValidationResult(check="scheduling_and_bed", passed=True),
        status="awaiting_approval",
    ))
    feedback = AsyncMock()
    monkeypatch.setattr("app.graph.coordinator.run_scheduling_bed_agent", scheduling)
    monkeypatch.setattr("app.graph.coordinator.run_feedback_support", feedback)
    preferred = (datetime.now(UTC).date() + timedelta(days=3)).isoformat()

    response = await coordinate(CoordinatorRequest(
        objective="Reserve a ward bed for tomorrow's abhyanga admission.",
        context={
            "patient_id": str(uuid4()),
            "treatment_id": str(uuid4()),
            "ward_id": str(uuid4()),
            "preferred_date": preferred,
        },
    ))

    scheduling.assert_awaited_once()
    feedback.assert_not_awaited()
    assert response.delegated_to is Specialist.SCHEDULING_BED
    assert response.workflow_id == scheduling.return_value.workflow_id
    assert response.approval_status == "pending"
    assert response.final_outcome == "awaiting_approval"


async def test_intake_routes_feedback_objective_to_feedback_support(monkeypatch):
    _ollama(monkeypatch, "feedback_support")
    feedback_id = str(uuid4())
    scheduling = AsyncMock()
    feedback = AsyncMock(return_value=FeedbackAgentResponse(
        sentiment=None,
        category=None,
        priority=None,
        similar_feedback_count=None,
        suggested_reply="We will review the shirodhara wait with the therapist.",
        draft_skipped=False,
        workflow_id=str(uuid4()),
        status="awaiting_review",
    ))
    monkeypatch.setattr("app.graph.coordinator.run_scheduling_bed_agent", scheduling)
    monkeypatch.setattr("app.graph.coordinator.run_feedback_support", feedback)

    response = await coordinate(CoordinatorRequest(
        objective="The patient left a comment about the crowded nadi pariksha waiting area.",
        context={
            "feedback_id": feedback_id,
            "patient_id": str(uuid4()),
            "comment_text": "The waiting area was crowded after nadi pariksha.",
        },
    ))

    feedback.assert_awaited_once()
    scheduling.assert_not_awaited()
    assert feedback.await_args.args[0].feedback_id == feedback_id
    assert response.delegated_to is Specialist.FEEDBACK_SUPPORT
    assert response.workflow_id == feedback.return_value.workflow_id
    assert response.final_outcome == "awaiting_review"
