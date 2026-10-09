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


_BLANK_VALUES = {"", "none", "null", "n/a", "na", "not provided", "not evaluated", "not recorded"}


def _recorded(value: Any) -> str | None:
    if value is None:
        return None
    text = str(value).strip()
    if text.lower() in _BLANK_VALUES:
        return None
    return text


def _patient_name(record: dict[str, Any]) -> str | None:
    full_name = _recorded(record.get("fullName"))
    if full_name:
        return full_name
    first_name = record.get("firstName") or ""
    last_name = record.get("lastName") or ""
    return _recorded(f"{first_name} {last_name}".strip())


def _answer_from_record(question: str, record: dict[str, Any]) -> str:
    """Reply with only the profile field the question names."""
    text = question.lower()
    address = _recorded(record.get("address"))

    if "prakriti" in text:
        value = _recorded(record.get("prakriti"))
        return f"Your registered Prakriti is {value}." if value else "Your registered Prakriti is not recorded on your profile."
    if "vikriti" in text:
        value = _recorded(record.get("vikriti"))
        return f"Your registered Vikriti is {value}." if value else "Your registered Vikriti is not recorded on your profile."
    if "blood" in text:
        value = _recorded(record.get("bloodGroup"))
        return f"Your recorded blood group is {value}." if value else "Your blood group is not recorded on your profile."
    if "allerg" in text:
        value = _recorded(record.get("allergies"))
        return f"Your recorded allergies are {value}." if value else "No allergies are recorded on your profile."
    if any(word in text for word in ("phone", "mobile", "contact number")):
        value = _recorded(record.get("phone"))
        return f"The phone number on your profile is {value}." if value else "No phone number is recorded on your profile."
    if "email" in text:
        value = _recorded(record.get("email"))
        return f"The email on your profile is {value}." if value else "No email is recorded on your profile."
    if "uhid" in text or "patient id" in text:
        value = _recorded(record.get("uhid"))
        return f"Your UHID is {value}." if value else "Your UHID is not recorded on your profile."
    if any(word in text for word in ("date of birth", "birthday", "born")):
        value = _recorded(record.get("dateOfBirth"))
        return f"Your date of birth on file is {value}." if value else "Your date of birth is not recorded on your profile."
    if "gender" in text:
        value = _recorded(record.get("gender"))
        return f"Your recorded gender is {value}." if value else "Your gender is not recorded on your profile."
    if re.search(r"\bname\b", text):
        value = _patient_name(record)
        return f"The name on your profile is {value}." if value else "Your name is not recorded on your profile."
    if any(word in text for word in ("district", "address", "where")):
        if not address:
            return "Your profile does not have a registered address, so the district is not recorded."
        return f"Your registered address is {address}."
    if "regist" in text and "date" in text:
        value = _recorded(record.get("registeredAt"))
        return f"Your registration date is {value}." if value else "Your registration date is not recorded on your profile."

    return f"Here is the administrative information from your record:\n\n{_format_patient_context(record)}"


async def _answer_node(state: PatientInfoState) -> PatientInfoState:
    """Answer from the saved administrative record. The phone cannot wait for a model."""
    if state.get("answer"):
        return state

    record = state.get("patient_record")
    if not record:
        return {
            **state,
            "answer": "No administrative record available for this patient.",
        }

    return {**state, "answer": _answer_from_record(state.get("question") or "", record)}


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
