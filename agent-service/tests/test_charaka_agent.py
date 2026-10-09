"""Charaka answers Ayurveda questions in conversation, and declines the rest."""

from __future__ import annotations

import pytest
from fastapi.testclient import TestClient

from app.agents.charaka_agent import (
    concern_reply,
    is_obviously_outside_ayurveda,
    parse_charaka_reply,
    run_charaka_agent,
)
from app.main import app
from app.schemas import CharakaAgentRequest, CharakaTurn, TreatmentScheduleItem, TreatmentScheduleToolOutput
from app.settings import settings


def test_obvious_non_ayurveda_is_caught_before_the_model():
    assert is_obviously_outside_ayurveda("What is the capital of France?")
    assert is_obviously_outside_ayurveda("Write a python script to sort a list")
    assert not is_obviously_outside_ayurveda("What does a Pitta prakriti feel like?")
    assert not is_obviously_outside_ayurveda("Is the weather a reason Vata rises in winter?")


def _hospital_list() -> list[TreatmentScheduleItem]:
    return [
        TreatmentScheduleItem(
            id="1",
            name="Shirodhara",
            description="Continuous stream of warm medicated oil on the forehead; indicated for vata-related restlessness and sleep disturbance.",
            duration_minutes=45,
            unit_price=2200,
        ),
        TreatmentScheduleItem(
            id="2",
            name="Abhyanga",
            description="Full-body herbal oil massage.",
            duration_minutes=60,
            unit_price=1800,
        ),
        TreatmentScheduleItem(
            id="3",
            name="Nasya Treatment",
            description="Nasal administration of medicated oils for shiro-roga and kapha accumulated in the head and sinuses.",
            duration_minutes=30,
            unit_price=1200,
        ),
        TreatmentScheduleItem(
            id="4",
            name="Herbal Steam Therapy",
            description="Swedana with herbal steam to loosen ama, open srotas, and prepare the body for panchakarma.",
            duration_minutes=30,
            unit_price=1500,
        ),
    ]


def test_stress_question_names_the_therapies_that_fit():
    answer = concern_reply("What therapies do you offer for stress and relaxation?", _hospital_list())
    assert answer is not None
    assert "Shirodhara" in answer
    assert "Abhyanga" in answer
    assert "couldn't find" not in answer.lower()
    assert "Nasya" not in answer


def test_a_different_concern_uses_the_same_path():
    sleep = concern_reply("What helps with poor sleep?", _hospital_list())
    sinus = concern_reply("What do you offer for sinus congestion?", _hospital_list())
    digestion = concern_reply("Which therapies are used when digestion is weak and there is ama?", _hospital_list())
    assert sleep is not None and "Shirodhara" in sleep
    assert sinus is not None and "Nasya" in sinus
    assert digestion is not None and "Herbal Steam" in digestion
    assert "Shirodhara" not in sinus


@pytest.mark.asyncio
async def test_concern_question_does_not_wait_for_the_model(monkeypatch):
    async def catalogue(query, question=""):
        return TreatmentScheduleToolOutput(query=query, treatments=_hospital_list())

    async def fail_chat(messages):
        raise AssertionError("a concern question should be answered from the therapy list")

    monkeypatch.setattr("app.agents.charaka_agent.get_treatment_schedule", catalogue)
    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", fail_chat)
    response = await run_charaka_agent(
        CharakaAgentRequest(question="What therapies do you offer for stress and relaxation?")
    )
    assert response.refused is False
    assert "Shirodhara" in response.answer


def test_scope_marker_is_not_spoken():
    outside, answer = parse_charaka_reply(
        "OUTSIDE\nThat's outside what I talk about.",
        "Who won the match?",
    )
    assert outside is True
    assert "OUTSIDE" not in answer
    assert "outside what I talk about" in answer

    in_scope, spoken = parse_charaka_reply("AYURVEDA\nPitta is the heat in you.", "What is pitta?")
    assert in_scope is False
    assert spoken == "Pitta is the heat in you."


@pytest.mark.asyncio
async def test_outside_question_is_declined_without_calling_the_model(monkeypatch):
    async def fail_chat(messages):
        raise AssertionError("the model should not be called")

    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", fail_chat)
    response = await run_charaka_agent(CharakaAgentRequest(question="What is the capital of France?"))
    assert response.refused is True
    assert "Ayurveda" in response.answer
    assert response.workflow_id


@pytest.mark.asyncio
async def test_ayurveda_question_is_answered_in_a_spoken_voice(monkeypatch):
    async def chat(messages):
        assert messages[0]["role"] == "system"
        assert messages[-1] == {"role": "user", "content": "What is triphala used for in Ayurveda?"}
        return "AYURVEDA\nTriphala is three fruits we use to keep digestion easy. I would not hand you a dose from here."

    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", chat)
    response = await run_charaka_agent(
        CharakaAgentRequest(question="What is triphala used for in Ayurveda?")
    )
    assert response.refused is False
    assert response.answer.startswith("Triphala is three fruits")
    assert "AYURVEDA" not in response.answer


@pytest.mark.asyncio
async def test_follow_up_includes_the_earlier_chat(monkeypatch):
    seen: dict[str, list] = {}

    async def chat(messages):
        seen["messages"] = messages
        return "AYURVEDA\nThe oil we were talking about is sesame, warmed."

    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", chat)
    await run_charaka_agent(
        CharakaAgentRequest(
            question="And the oil?",
            history=[
                CharakaTurn(role="user", text="Tell me about abhyanga."),
                CharakaTurn(role="assistant", text="Abhyanga is a warm oil massage."),
            ],
        )
    )
    roles = [(message["role"], message["content"]) for message in seen["messages"]]
    assert ("user", "Tell me about abhyanga.") in roles
    assert ("assistant", "Abhyanga is a warm oil massage.") in roles
    assert roles[-1] == ("user", "And the oil?")


@pytest.mark.asyncio
async def test_hospital_fee_question_is_given_the_real_list(monkeypatch):
    seen: dict[str, str] = {}

    async def catalogue(query, question=""):
        return TreatmentScheduleToolOutput(
            query=query,
            treatments=[
                TreatmentScheduleItem(
                    id="t-1",
                    name="Panchakarma",
                    unit_price=12000,
                    available_days=["Monday", "Wednesday"],
                    duration_minutes=90,
                )
            ],
        )

    async def chat(messages):
        seen["system"] = messages[0]["content"]
        return "AYURVEDA\nPanchakarma here runs Monday and Wednesday, and the fee is Rs. 12000."

    monkeypatch.setattr("app.agents.charaka_agent.get_treatment_schedule", catalogue)
    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", chat)
    response = await run_charaka_agent(
        CharakaAgentRequest(question="When is Panchakarma available and what is the fee?")
    )
    assert "Panchakarma" in seen["system"]
    assert "12000" in seen["system"]
    assert response.refused is False
    assert "Monday" in response.answer


@pytest.mark.asyncio
async def test_a_quiet_model_still_gets_a_human_reply(monkeypatch):
    async def chat(messages):
        raise RuntimeError("ollama down")

    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", chat)
    response = await run_charaka_agent(CharakaAgentRequest(question="What is agni?"))
    assert response.refused is False
    assert "slow to arrive" in response.answer


def test_charaka_endpoint_requires_the_internal_secret():
    client = TestClient(app)
    response = client.post("/internal/agents/charaka", json={"question": "What is vata?"})
    assert response.status_code == 401


def test_charaka_endpoint_returns_the_spoken_answer(monkeypatch):
    async def chat(messages):
        return "AYURVEDA\nVata is the wind in the body, movement and breath."

    monkeypatch.setattr("app.agents.charaka_agent._ollama_chat", chat)
    client = TestClient(app)
    response = client.post(
        "/internal/agents/charaka",
        headers={"X-Internal-Secret": settings.shared_secret.get_secret_value()},
        json={
            "question": "What is vata?",
            "history": [{"role": "user", "text": "Can we talk about doshas?"}],
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["refused"] is False
    assert body["answer"].startswith("Vata is the wind")
    assert body["workflow_id"]
