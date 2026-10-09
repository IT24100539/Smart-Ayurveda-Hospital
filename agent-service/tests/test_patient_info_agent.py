"""Tests for the Patient Information Agent (Member 1).

Covers:
1. Grounded answer from internal GET /api/internal/patients/{id} endpoint.
2. Hard safety refusal: medical / diagnostic questions are refused outright
   with the fixed message and do NOT call the internal tool or the LLM.
3. FastAPI endpoint authentication and response contract for POST /internal/agents/patient-info.
"""

from __future__ import annotations

from typing import Any
from unittest.mock import AsyncMock, MagicMock, patch
from uuid import UUID, uuid4

import pytest
from fastapi.testclient import TestClient

from app.agents.patient_info_agent import (
    REFUSAL_MESSAGE,
    _answer_from_record,
    is_medical_question,
    run_patient_info_agent,
)
from app.main import app
from app.schemas import PatientInfoAgentRequest
from app.settings import settings

TEST_PATIENT_ID = uuid4()

MOCK_PATIENT_RECORD: dict[str, Any] = {
    "id": str(TEST_PATIENT_ID),
    "uhid": "AH-2026-0042",
    "firstName": "Sunil",
    "lastName": "Perera",
    "fullName": "Sunil Perera",
    "dateOfBirth": "1980-05-15",
    "gender": "Male",
    "phone": "+94771234567",
    "email": "sunil.perera@example.lk",
    "address": "45 Galle Road, Colombo 03, Western Province",
    "bloodGroup": "O+",
    "allergies": "Penicillin",
    "prakriti": "VataPitta",
    "vikriti": "Pitta",
    "isActive": True,
    "registeredAt": "2026-01-10T08:30:00Z",
}


def _make_httpx_response(json_data: dict[str, Any], status_code: int = 200):
    mock_resp = MagicMock()
    mock_resp.status_code = status_code
    mock_resp.json.return_value = json_data
    mock_resp.raise_for_status = MagicMock()
    return mock_resp


# ---------------------------------------------------------------------------
# Unit tests: Medical safety guard
# ---------------------------------------------------------------------------


def test_district_question_does_not_dump_the_whole_record():
    answer = _answer_from_record(
        "Which district is my profile registered under?",
        {"address": None, "phone": "0774218586", "prakriti": "None"},
    )
    assert "district is not recorded" in answer
    assert "0774218586" not in answer


def test_prakriti_question_reports_only_prakriti():
    answer = _answer_from_record(
        "What is my registered Prakriti type?",
        {"prakriti": "None", "phone": "0774218586", "uhid": "SAH-2026-00010"},
    )
    assert "Prakriti is not recorded" in answer
    assert "SAH-2026-00010" not in answer


class TestPatientInfoSafetyGuard:
    def test_administrative_questions_allowed(self):
        assert not is_medical_question("What district am I registered in?")
        assert not is_medical_question("What is my registered address?")
        assert not is_medical_question("What is my assigned UHID?")
        assert not is_medical_question("Which phone number do you have on file for me?")
        assert not is_medical_question("What is my recorded blood group?")

    def test_medical_advice_refused(self):
        assert is_medical_question("Should I take ashwagandha for my fever?")
        assert is_medical_question("Can you diagnose this pain in my lower back?")
        assert is_medical_question("What medication should I take for headache?")
        assert is_medical_question("Is panchakarma suitable for my heart condition?")
        assert is_medical_question("Please prescribe a remedy for my chronic joint ache.")
        assert is_medical_question("What illness do these symptoms indicate?")


# ---------------------------------------------------------------------------
# Integration tests
# ---------------------------------------------------------------------------


@pytest.mark.asyncio
async def test_grounded_administrative_question_returns_answer():
    """Administrative inquiry fetches internal record and returns grounded answer."""
    with patch("app.agents.patient_info_agent.httpx.AsyncClient") as mock_client_cls:
        mock_client = AsyncMock()
        mock_client.get = AsyncMock(return_value=_make_httpx_response(MOCK_PATIENT_RECORD))
        mock_client.__aenter__ = AsyncMock(return_value=mock_client)
        mock_client.__aexit__ = AsyncMock(return_value=None)
        mock_client_cls.return_value = mock_client

        request = PatientInfoAgentRequest(
            patient_id=TEST_PATIENT_ID,
            question="What district and address am I registered in?",
        )
        response = await run_patient_info_agent(request)

        assert response.refused is False
        assert "Colombo" in response.answer or "Galle Road" in response.answer
        mock_client.get.assert_called_once()
        # Verify call went to the internal patient endpoint
        call_url = mock_client.get.call_args[0][0]
        assert f"/api/internal/patients/{TEST_PATIENT_ID}" in call_url


@pytest.mark.asyncio
async def test_medical_question_refused_without_calling_tool_or_model():
    """Medical question triggers outright refusal without calling backend endpoint or LLM."""
    with patch("app.agents.patient_info_agent.httpx.AsyncClient") as mock_client_cls:
        request = PatientInfoAgentRequest(
            patient_id=TEST_PATIENT_ID,
            question="Can you diagnose my fever and recommend a treatment?",
        )
        response = await run_patient_info_agent(request)

        assert response.refused is True
        assert response.answer == REFUSAL_MESSAGE
        mock_client_cls.assert_not_called()


# ---------------------------------------------------------------------------
# API Route tests
# ---------------------------------------------------------------------------


def test_patient_info_endpoint_requires_internal_secret():
    client = TestClient(app)
    resp = client.post(
        "/internal/agents/patient-info",
        json={"patientId": str(TEST_PATIENT_ID), "question": "What is my UHID?"},
    )
    assert resp.status_code == 401


def test_patient_info_endpoint_with_valid_secret_and_refusal():
    client = TestClient(app)
    resp = client.post(
        "/internal/agents/patient-info",
        headers={"X-Internal-Secret": settings.shared_secret.get_secret_value()},
        json={
            "patientId": str(TEST_PATIENT_ID),
            "question": "Should I take medication for my chest pain?",
        },
    )
    assert resp.status_code == 200
    data = resp.json()
    assert data["refused"] is True
    assert data["answer"] == REFUSAL_MESSAGE
