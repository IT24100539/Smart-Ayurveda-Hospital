"""Tests for the Ayurvedic Treatment Information Agent.

Three scenarios:
1. Schedule question → grounded answer citing real seeded treatment days.
2. Nonsense / no-match question → "not found" rather than hallucinated answer.
3. Diagnostic-sounding question → refusal.

All external I/O (httpx to the hospital API) is mocked so the tests run without any servers.
"""

from __future__ import annotations

from typing import Any
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
from fastapi.testclient import TestClient

from app.agents.treatment_info_agent import (
    REFUSAL_MESSAGE,
    _catalogue_answer,
    is_medical_advice_question,
    run_treatment_info_agent,
)
from app.main import app
from app.schemas import TreatmentInfoAgentRequest
from app.settings import settings

# ---------------------------------------------------------------------------
# Fixtures: mock backend responses
# ---------------------------------------------------------------------------

SEED_PANCHAKARMA_RESPONSE: dict[str, Any] = {
    "items": [
        {
            "id": "aaaaaaaa-0000-0000-0000-000000000001",
            "name": "Panchakarma",
            "nameSinhala": "පංචකර්ම",
            "description": (
                "Supervised five-fold shodhana programme (vamana, virechana, "
                "basti, nasya, raktamokshana as indicated) to clear ama and "
                "restore dosha balance."
            ),
            "descriptionSinhala": "",
            "category": 0,
            "durationMinutes": 90,
            "unitPrice": 4500.0,
            "isActive": True,
            "availableDays": ["Monday", "Wednesday", "Friday"],
        }
    ],
    "totalCount": 1,
    "page": 1,
    "pageSize": 20,
}

EMPTY_RESPONSE: dict[str, Any] = {
    "items": [],
    "totalCount": 0,
    "page": 1,
    "pageSize": 20,
}


def _make_httpx_response(json_data: dict[str, Any], status_code: int = 200):
    """Create a mock httpx.Response."""
    mock_resp = MagicMock()
    mock_resp.status_code = status_code
    mock_resp.json.return_value = json_data
    mock_resp.raise_for_status = MagicMock()
    return mock_resp


# ---------------------------------------------------------------------------
# Unit tests for the medical-advice guard
# ---------------------------------------------------------------------------


def test_fee_question_states_only_the_fee():
    treatments = [
        {"id": "1", "name": "Panchakarma", "available_days": ["Monday", "Wednesday"], "unit_price": 4500, "duration_minutes": 90},
        {"id": "2", "name": "Nasya", "available_days": ["Tuesday"], "unit_price": 1200, "duration_minutes": 30},
    ]
    answer = _catalogue_answer("what is the cost of panchakarma treatment", treatments)
    assert answer == "The fee for Panchakarma is Rs. 4500."
    assert "Nasya" not in answer


def test_unknown_topic_does_not_list_the_catalogue():
    treatments = [
        {"id": "1", "name": "Panchakarma", "available_days": ["Monday"], "unit_price": 4500, "duration_minutes": 90},
    ]
    answer = _catalogue_answer("what therapies do you offer for relaxation", treatments)
    assert "Panchakarma" not in answer
    assert "couldn't find" in answer


def test_weekday_question_lists_only_therapies_offered_that_day():
    treatments = [
        {"id": "1", "name": "Abhyanga", "available_days": ["Monday"], "unit_price": 1800, "duration_minutes": 60},
        {"id": "2", "name": "Panchakarma", "available_days": ["Monday", "Wednesday", "Friday"], "unit_price": 4500, "duration_minutes": 90},
        {"id": "3", "name": "Nasya", "available_days": ["Tuesday", "Thursday"], "unit_price": 1200, "duration_minutes": 30},
    ]
    answer = _catalogue_answer("what are the treatments available on Wednesday", treatments)
    assert "Panchakarma" in answer
    assert "Abhyanga" not in answer
    assert "Nasya" not in answer
    assert "Wednesday" in answer


class TestMedicalAdviceGuard:
    def test_schedule_question_is_not_medical(self):
        assert not is_medical_advice_question("When is Panchakarma available?")

    def test_fee_question_is_not_medical(self):
        assert not is_medical_advice_question("How much does Shirodhara cost?")

    def test_should_i_is_medical(self):
        assert is_medical_advice_question("Should I get Panchakarma for my arthritis?")

    def test_diagnose_is_medical(self):
        assert is_medical_advice_question("Can you diagnose my condition?")

    def test_treatment_for_condition_is_medical(self):
        assert is_medical_advice_question(
            "What treatment for my back pain do you recommend?"
        )

    def test_cure_for_is_medical(self):
        assert is_medical_advice_question("Is there a cure for my insomnia?")

    def test_prescribe_is_medical(self):
        assert is_medical_advice_question("Please prescribe something for my headache")


# ---------------------------------------------------------------------------
# Integration tests (with mocked I/O)
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_schedule_question_returns_grounded_answer():
    """A question about Panchakarma schedule should return real seeded days."""
    with patch("app.agents.treatment_info_agent.httpx.AsyncClient") as mock_client_cls:
        mock_client = AsyncMock()
        mock_client.get = AsyncMock(
            return_value=_make_httpx_response(SEED_PANCHAKARMA_RESPONSE)
        )
        mock_client.__aenter__ = AsyncMock(return_value=mock_client)
        mock_client.__aexit__ = AsyncMock(return_value=None)
        mock_client_cls.return_value = mock_client

        request = TreatmentInfoAgentRequest(
            question="When is Panchakarma available?"
        )
        response = await run_treatment_info_agent(request)

    assert response.refused is False
    assert len(response.matched_treatment_ids) > 0
    assert "aaaaaaaa-0000-0000-0000-000000000001" in response.matched_treatment_ids

    answer_lower = response.answer.lower()
    assert "monday" in answer_lower
    assert "wednesday" in answer_lower
    assert "friday" in answer_lower


@pytest.mark.asyncio
async def test_nonsense_question_returns_not_found():
    """A query that matches nothing should say 'not found', never hallucinate."""
    with patch(
        "app.agents.treatment_info_agent.httpx.AsyncClient"
    ) as mock_client_cls:
        mock_client = AsyncMock()
        mock_client.get = AsyncMock(
            return_value=_make_httpx_response(EMPTY_RESPONSE)
        )
        mock_client.__aenter__ = AsyncMock(return_value=mock_client)
        mock_client.__aexit__ = AsyncMock(return_value=None)
        mock_client_cls.return_value = mock_client

        request = TreatmentInfoAgentRequest(
            question="Do you offer quantum chakra realignment therapy?"
        )
        response = await run_treatment_info_agent(request)

    assert response.refused is False
    assert len(response.matched_treatment_ids) == 0

    answer_lower = response.answer.lower()
    assert any(
        phrase in answer_lower
        for phrase in ("couldn't find", "no matching", "not found", "not currently")
    ), f"Expected a 'not found' message, got: {response.answer}"


@pytest.mark.asyncio
async def test_diagnostic_question_triggers_refusal():
    """Medical-advice questions must be refused before any tool call."""
    # No need to mock httpx — the classify node short-circuits
    request = TreatmentInfoAgentRequest(
        question="Should I get Panchakarma for my arthritis?"
    )
    response = await run_treatment_info_agent(request)

    assert response.refused is True
    assert response.answer == REFUSAL_MESSAGE
    assert len(response.matched_treatment_ids) == 0


@pytest.mark.asyncio
async def test_days_question_and_sinhala_question_returns_grounded_answer():
    """Questions with 'days' or in Sinhala should resolve treatments correctly."""
    with patch("app.agents.treatment_info_agent.httpx.AsyncClient") as mock_client_cls:
        mock_client = AsyncMock()
        mock_client.get = AsyncMock(
            return_value=_make_httpx_response(SEED_PANCHAKARMA_RESPONSE)
        )
        mock_client.__aenter__ = AsyncMock(return_value=mock_client)
        mock_client.__aexit__ = AsyncMock(return_value=None)
        mock_client_cls.return_value = mock_client

        # 1. Multi-word inquiry with 'days'
        req1 = TreatmentInfoAgentRequest(question="What days is Panchakarma available?")
        res1 = await run_treatment_info_agent(req1)
        assert res1.refused is False
        assert "aaaaaaaa-0000-0000-0000-000000000001" in res1.matched_treatment_ids

        # 2. Sinhala inquiry
        req2 = TreatmentInfoAgentRequest(question="පංචකර්ම ගැන විස්තර කියන්න")
        res2 = await run_treatment_info_agent(req2)
        assert res2.refused is False
        assert "aaaaaaaa-0000-0000-0000-000000000001" in res2.matched_treatment_ids


# ---------------------------------------------------------------------------
# Route-level tests (via TestClient)
# ---------------------------------------------------------------------------

INTERNAL_SECRET = settings.shared_secret.get_secret_value()


class TestTreatmentInfoRoute:
    """Tests for POST /internal/agents/treatment-info."""

    def test_requires_internal_secret(self):
        client = TestClient(app)
        resp = client.post(
            "/internal/agents/treatment-info",
            json={"question": "When is Shirodhara available?"},
        )
        assert resp.status_code == 401

    def test_wrong_secret_rejected(self):
        client = TestClient(app)
        resp = client.post(
            "/internal/agents/treatment-info",
            json={"question": "When is Shirodhara available?"},
            headers={"X-Internal-Secret": "wrong-secret"},
        )
        assert resp.status_code == 401

    def test_refusal_via_route(self):
        """Diagnostic question via the HTTP route returns refusal."""
        client = TestClient(app)
        resp = client.post(
            "/internal/agents/treatment-info",
            json={"question": "Should I take Nasya for my sinusitis?"},
            headers={"X-Internal-Secret": INTERNAL_SECRET},
        )
        assert resp.status_code == 200
        body = resp.json()
        assert body["refused"] is True
        assert body["answer"] == REFUSAL_MESSAGE
        assert body["matched_treatment_ids"] == []
