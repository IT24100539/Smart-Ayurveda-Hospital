"""Patient Information Agent.

Patient-facing agent that answers questions about their OWN administrative
record (e.g. registration details, contact info, assigned UHID, district)
using ONLY verified data from the backend ``GET /api/internal/patients/{id}``
endpoint guarded by the shared internal service key.

Strict safety rule:
Any medical, diagnostic, or treatment-suitability-sounding question is
refused outright with a fixed message directing the patient to speak with
hospital staff in person. No medical guessing, ever.
"""

from __future__ import annotations

import logging
import re
from typing import Any, TypedDict
from uuid import UUID, uuid4

import httpx
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_ollama import ChatOllama
from langgraph.graph import END, StateGraph

from app.schemas import (
    PatientInfoAgentRequest,
    PatientInfoAgentResponse,
    ToolResult,
    ValidationResult,
    WorkflowState,
)
from app.settings import settings
from app.state_store import get_state_store, persist, reset_persistence_warning
from app.tools.tools import _internal_headers

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Medical / Diagnostic safety guard
# ---------------------------------------------------------------------------

MEDICAL_ADVICE_PATTERNS: list[re.Pattern[str]] = [
    re.compile(r"\bshould\s+i\b", re.IGNORECASE),
    re.compile(r"\bis\s+(?:it|this)\s+(?:good|safe|suitable|right)\s+for\b", re.IGNORECASE),
    re.compile(r"\bcan\s+(?:i|you)\s+(?:recommend|suggest)\b", re.IGNORECASE),
    re.compile(r"\bdiagnos[ei]", re.IGNORECASE),
    re.compile(r"\bprescri(?:be|ption)\b", re.IGNORECASE),
    re.compile(r"\bcure\s+for\b", re.IGNORECASE),
    re.compile(r"\btreat(?:ment)?\s+for\s+(?:my|a|the)\b", re.IGNORECASE),
    re.compile(r"\bsuitable\s+for\s+(?:my|a|the)\b", re.IGNORECASE),
    re.compile(r"\bwill\s+(?:it|this)\s+(?:help|cure|heal)\b", re.IGNORECASE),
    re.compile(r"\bwhat\s+(?:should|can)\s+i\s+(?:take|use|do)\s+for\b", re.IGNORECASE),
    re.compile(r"\bmedical\s+advice\b", re.IGNORECASE),
    re.compile(r"\bam\s+i\s+(?:suitable|eligible)\b", re.IGNORECASE),
    re.compile(r"\bdo\s+i\s+(?:need|require)\b", re.IGNORECASE),
    re.compile(r"\b(symptom|illness|disease|infection|pain|ache|fever|cough|dose|dosage|medicine|medication)\b", re.IGNORECASE),
]

REFUSAL_MESSAGE = (
    "I cannot provide medical, diagnostic, or treatment advice. "
    "Please speak with our hospital staff or an Ayurvedic physician directly for any medical or treatment-related guidance."
)


def is_medical_question(question: str) -> bool:
    """Return True if question asks for medical advice, diagnosis, or treatment suitability."""
    return any(pattern.search(question) for pattern in MEDICAL_ADVICE_PATTERNS)


# ---------------------------------------------------------------------------
# State definition
# ---------------------------------------------------------------------------


class PatientInfoState(TypedDict, total=False):
    workflow_id: str
    patient_id: str
    question: str
    patient_record: dict[str, Any] | None
    refused: bool
    answer: str


def _jsonable(obj: Any) -> Any:
    if isinstance(obj, dict):
        return {k: _jsonable(v) for k, v in obj.items() if v is not None}
    if isinstance(obj, list):
        return [_jsonable(v) for v in obj]
    if isinstance(obj, (str, int, float, bool)):
        return obj
    return str(obj)


def _tracked(step: str, node):
    async def transition(state: PatientInfoState) -> PatientInfoState:
        changes = node(state)
        if hasattr(changes, "__await__"):
            changes = await changes
        workflow_id = state.get("workflow_id")
        if workflow_id:
            store = get_state_store()
            current = await persist(store.get(workflow_id))
            completed = [*(current.completed_steps if current else []), step]
            tool_results = [
                *(current.tool_results if current else []),
                ToolResult(
                    tool=step,
                    succeeded=not bool(changes.get("refused")),
                    output=_jsonable(changes),
                ),
            ]
            await persist(store.update(workflow_id, completed_steps=completed, tool_results=tool_results))
        return changes

    return transition


# ---------------------------------------------------------------------------
# Graph Nodes
# ---------------------------------------------------------------------------


def _classify_node(state: PatientInfoState) -> PatientInfoState:
    """First node: detect medical/diagnostic questions and refuse immediately."""
    question = state.get("question", "")
    if is_medical_question(question):
        return {
            **state,
            "refused": True,
            "answer": REFUSAL_MESSAGE,
        }
    return {**state, "refused": False}


def _should_continue(state: PatientInfoState) -> str:
    """Conditional edge: skip tool call and answer generation if refused."""
    return "end" if state.get("refused") else "fetch_patient"


async def _fetch_patient_node(state: PatientInfoState) -> PatientInfoState:
    """Call GET /api/internal/patients/{id} with X-Internal-Service-Key."""
    patient_id = state["patient_id"]
    url = f"{settings.hospital_api_base_url.rstrip('/')}/api/internal/patients/{patient_id}"
    headers = _internal_headers()

    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            resp = await client.get(url, headers=headers)
            if resp.status_code == 404:
                return {
                    **state,
                    "patient_record": None,
                    "answer": "Patient record not found. Please contact hospital staff to verify your registration.",
                }
            resp.raise_for_status()
            record = resp.json()
            return {
                **state,
                "patient_record": record,
            }
    except Exception as exc:
        logger.warning("Failed to fetch internal patient record %s: %s", patient_id, exc)
        return {
            **state,
            "patient_record": None,
            "answer": "Unable to access patient record at this time. Please try again later or consult staff.",
        }


def _format_patient_context(record: dict[str, Any]) -> str:
    first_name = record.get("firstName") or ""
    last_name = record.get("lastName") or ""
    fallback_name = f"{first_name} {last_name}".strip()
    full_name = record.get("fullName") or fallback_name

    lines: list[str] = [
        f"UHID: {record.get('uhid', 'N/A')}",
        f"Name: {full_name}",
        f"Date of Birth: {record.get('dateOfBirth', 'N/A')}",
        f"Gender: {record.get('gender', 'N/A')}",
        f"Phone: {record.get('phone', 'N/A')}",
        f"Email: {record.get('email') or 'Not provided'}",
        f"Address: {record.get('address') or 'Not provided'}",
        f"Blood Group: {record.get('bloodGroup') or 'Not provided'}",
        f"Allergies: {record.get('allergies') or 'None reported'}",
        f"Prakriti: {record.get('prakriti') or 'Not evaluated'}",
        f"Vikriti: {record.get('vikriti') or 'Not evaluated'}",
        f"Registration Date: {record.get('registeredAt', 'N/A')}",
        f"Account Status: {'Active' if record.get('isActive', True) else 'Inactive'}",
    ]
    return "\n".join(lines)


_SYSTEM_PROMPT = """\
You are an administrative assistant for Smart Ayurveda Hospital. Your role is to answer a patient's questions about their OWN administrative record.

STRICT GROUNDING RULES:
1. You may ONLY use the verified patient record provided below.
2. NEVER invent, extrapolate, or guess any details (such as dates, addresses, phone numbers, UHID, or blood group).
3. If the requested information is present in the record, quote or state it clearly and accurately.
4. If the requested information is NOT in the record, state that it is not present in their hospital administrative profile.
5. NEVER provide medical advice, diagnosis, prognosis, or treatment recommendations.
6. Keep your answer polite, concise, and helpful.

PATIENT RECORD:
{patient_record}
"""


async def _answer_node(state: PatientInfoState) -> PatientInfoState:
    """Generate grounded answer strictly based on the retrieved patient record."""
    if state.get("answer"):
        # Already set (e.g. record not found or error)
        return state

    record = state.get("patient_record")
    if not record:
        return {
            **state,
            "answer": "No administrative record available for this patient.",
        }

    formatted_record = _format_patient_context(record)
    system_msg = _SYSTEM_PROMPT.format(patient_record=formatted_record)

    try:
        import asyncio
        llm = ChatOllama(
            base_url=settings.ollama_base_url,
            model=settings.ollama_model,
            temperature=0.1,
        )
        response = await asyncio.wait_for(
            llm.ainvoke([
                SystemMessage(content=system_msg),
                HumanMessage(content=state["question"]),
            ]),
            timeout=settings.ollama_timeout_seconds,
        )
        answer = response.content
    except Exception:
        # Fallback: if Ollama is unavailable, answer with a structured summary directly from record
        answer = f"Here is the administrative information from your record:\n\n{formatted_record}"

    return {**state, "answer": answer}


# ---------------------------------------------------------------------------
# Graph Compilation
# ---------------------------------------------------------------------------


def _build_patient_info_graph():
    graph = StateGraph(PatientInfoState)

    graph.add_node("classify", _tracked("classify", _classify_node))
    graph.add_node("fetch_patient", _tracked("fetch_patient", _fetch_patient_node))
    graph.add_node("compose_answer", _tracked("compose_answer", _answer_node))

    graph.set_entry_point("classify")
    graph.add_conditional_edges(
        "classify",
        _should_continue,
        {"fetch_patient": "fetch_patient", "end": END},
    )
    graph.add_edge("fetch_patient", "compose_answer")
    graph.add_edge("compose_answer", END)

    return graph.compile()


_GRAPH = _build_patient_info_graph()


# ---------------------------------------------------------------------------
# Public Entrypoint
# ---------------------------------------------------------------------------


async def run_patient_info_agent(
    request: PatientInfoAgentRequest,
) -> PatientInfoAgentResponse:
    """Invoke the patient-info LangGraph and return a typed response."""
    reset_persistence_warning()
    workflow_id = str(uuid4())
    store = get_state_store()
    objective = request.question.strip() or "Patient administrative record question"

    await persist(store.save(
        WorkflowState(
            workflow_id=workflow_id,
            objective=objective,
            plan=["classify", "fetch_patient", "compose_answer"],
            approval_status=None,
            agent_name="patient_info",
        )
    ))

    result = await _GRAPH.ainvoke(
        {
            "workflow_id": workflow_id,
            "patient_id": str(request.patient_id),
            "question": request.question,
            "patient_record": None,
            "refused": False,
            "answer": "",
        }
    )

    refused = bool(result.get("refused"))
    changes: dict[str, Any] = {
        "final_outcome": "safe_failure" if refused else "success",
        "completed_steps": ["classify"] if refused else ["classify", "fetch_patient", "compose_answer"],
        "validation_results": [
            ValidationResult(
                check="medical_safety" if refused else "grounded_patient_record",
                passed=not refused,
                detail=REFUSAL_MESSAGE if refused else "Answer grounded in internal patient record only.",
            )
        ],
    }
    if refused:
        changes["errors"] = ["Medical advice and diagnostic questions are refused."]
    else:
        changes["related_entity_type"] = "Patient"
        changes["related_entity_id"] = str(request.patient_id)

    await persist(store.update(workflow_id, **changes))

    return PatientInfoAgentResponse(
        answer=result.get("answer") or "",
        refused=refused,
        workflow_id=workflow_id,
    )
