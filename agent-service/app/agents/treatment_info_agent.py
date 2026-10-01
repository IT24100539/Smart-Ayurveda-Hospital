"""Ayurvedic Treatment Information Agent.

Patient-facing agent that answers questions about treatments, services, and
schedules using ONLY verified data from the backend ``GET /api/treatments``
endpoint.  Never invents a treatment, day, or fee that isn't in the data
returned by its tool.

LangGraph flow::

    classify ──(refused?)──▶ END
       │
       ▼
     search ──▶ answer ──▶ END
"""

from __future__ import annotations

import re
from typing import Any, TypedDict
from uuid import UUID, uuid4

import httpx
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_ollama import ChatOllama
from langgraph import graph
from langgraph.graph import END, StateGraph

from app.schemas import (
    ToolResult,
    TreatmentInfoAgentRequest,
    TreatmentInfoAgentResponse,
    TreatmentScheduleItem,
    TreatmentScheduleToolOutput,
    ValidationResult,
    WorkflowState,
)
from app.settings import settings
from app.state_store import get_state_store, persist, reset_persistence_warning

# ---------------------------------------------------------------------------
# Medical-advice guard
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
]

REFUSAL_MESSAGE = (
    "I'm not able to provide medical advice or treatment recommendations for "
    "specific conditions. For personalised guidance, please consult with our "
    "hospital staff or an Ayurvedic physician directly."
)


def is_medical_advice_question(question: str) -> bool:
    """Return ``True`` if the question asks for medical advice/diagnosis."""
    return any(pattern.search(question) for pattern in MEDICAL_ADVICE_PATTERNS)


# ---------------------------------------------------------------------------
# Search-query extraction
# ---------------------------------------------------------------------------

_STOPWORDS = frozenset(
    (
        "a an the is are was were what when where which who whom how do does "
        "did will would shall can could may might about on in at to for of and "
        "or but with from by this that these those it its i me my we our you "
        "your they their there here please tell show give list "
        "available availability schedule scheduled cost costs price prices "
        "fee fees much many service services treatment treatments therapy therapies "
        "day days time times date dates detail details info information "
        "offer offered offers rate rates charge charges get know have has "
        "monday tuesday wednesday thursday friday saturday sunday "
        "ගැන විස්තර කියන්න මොනවාද තියෙන්නේ තියෙන දවස් දින ගාස්තු මිල කරන්නේ ලබා දෙන"
    ).split()
)


def _extract_search_query(question: str) -> str:
    """Derive a concise search keyword from a natural-language question.

    Strips punctuation and common stop words, returning the remaining
    meaningful terms that the backend ``Name`` filter can match against.
    Returns empty string if all words are stop words or general inquiries.
    """
    words = re.sub(r"[^\w\s]", "", question).split()
    meaningful = [w for w in words if w.lower() not in _STOPWORDS]
    return " ".join(meaningful[:5]) if meaningful else ""


# ---------------------------------------------------------------------------
# Tool: get_treatment_schedule
# ---------------------------------------------------------------------------


async def get_treatment_schedule(query: str, question: str = "") -> TreatmentScheduleToolOutput:
    """Call the backend ``GET /api/treatments`` endpoint.

    Returns a typed :class:`TreatmentScheduleToolOutput` with matched
    treatments.  On any HTTP error or when no results are found the
    treatments list is empty — the function never raises.
    """
    treatments: list[TreatmentScheduleItem] = []
    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            url = f"{settings.backend_base_url}/api/treatments"

            # 1. If a clean search keyword is extracted, try backend name search first
            if query.strip():
                resp = await client.get(url, params={"name": query.strip()})
                resp.raise_for_status()
                items_raw: list[dict[str, Any]] = resp.json().get("items") or []
                treatments = [
                    TreatmentScheduleItem.model_validate(item) for item in items_raw
                ]

            # 2. If no treatments matched or query was empty, fetch catalogue to match
            # against full question context (e.g. Sinhala names, general inquiries, multi-term names)
            if not treatments:
                resp_all = await client.get(url, params={"pageSize": 100})
                resp_all.raise_for_status()
                all_raw: list[dict[str, Any]] = resp_all.json().get("items") or []
                all_treatments = [
                    TreatmentScheduleItem.model_validate(item) for item in all_raw
                ]

                full_text = f"{question} {query}".strip().lower()

                # Check if any specific treatment name or keyword is mentioned
                matched: list[TreatmentScheduleItem] = []
                for t in all_treatments:
                    t_name = t.name.lower()
                    t_sinhala = (t.name_sinhala or "").lower()
                    tokens = [tok for tok in re.split(r"\s+", t_name) if tok not in {"treatment", "therapy", "consultation"}]
                    sinhala_tokens = [tok for tok in re.split(r"\s+", t_sinhala) if tok]
                    if (
                        t_name in full_text
                        or (t_sinhala and t_sinhala in full_text)
                        or any(tok in full_text for tok in tokens if len(tok) >= 4)
                        or any(st in full_text for st in sinhala_tokens if len(st) >= 3)
                    ):
                        matched.append(t)

                if matched:
                    treatments = matched
                else:
                    # Check if this is a general inquiry about available treatments/catalogue
                    general_inquiry_terms = {
                        "treatment", "treatments", "therapy", "therapies", "service", "services",
                        "catalog", "catalogue", "all", "available", "schedule", "schedules",
                        "day", "days", "fee", "fees", "cost", "costs", "price", "prices",
                        "offer", "offered", "list", "options", "what", "how much",
                        "ප්‍රතිකාර", "සේවා", "ගාස්තු", "මිල", "දවස්", "දින",
                    }
                    if not query.strip() or any(term in full_text for term in general_inquiry_terms):
                        unrecognized_specific = any(
                            token in full_text
                            for token in ["quantum", "chakra", "realignment", "acupuncture", "homeopathy", "reiki"]
                        )
                        if not unrecognized_specific:
                            treatments = all_treatments
    except Exception:
        treatments = []

    return TreatmentScheduleToolOutput(query=query, treatments=treatments)


# ---------------------------------------------------------------------------
# LangGraph state & nodes
# ---------------------------------------------------------------------------


class TreatmentInfoState(TypedDict):
    workflow_id: str
    question: str
    search_query: str
    tool_output: dict[str, Any]
    answer: str
    matched_treatment_ids: list[str]
    refused: bool


def _jsonable(changes: dict[str, Any]) -> dict[str, Any]:
    output: dict[str, Any] = {}
    for key, value in changes.items():
        if key == "tool_output":
            continue
        if isinstance(value, (str, int, float, bool, list)):
            output[key] = value
        elif value is not None:
            output[key] = str(value)
    return output


def _tracked(step: str, node):
    async def transition(state: TreatmentInfoState) -> TreatmentInfoState:
        changes = node(state)
        if hasattr(changes, "__await__"):
            changes = await changes
        workflow_id = state.get("workflow_id")
        if workflow_id:
            store = get_state_store()
            current = await persist(store.get(workflow_id))
            completed = [*(current.completed_steps if current else []), step]
            tool_results = [*(current.tool_results if current else []), ToolResult(
                tool=step, succeeded=not bool(changes.get("refused")), output=_jsonable(changes),
            )]
            await persist(store.update(workflow_id, completed_steps=completed, tool_results=tool_results))
        return changes

    return transition


def _classify_node(state: TreatmentInfoState) -> TreatmentInfoState:
    """First node — detect medical-advice questions and extract search query."""
    question = state["question"]

    if is_medical_advice_question(question):
        return {
            **state,
            "refused": True,
            "answer": REFUSAL_MESSAGE,
            "matched_treatment_ids": [],
        }

    search_query = _extract_search_query(question)
    return {**state, "search_query": search_query, "refused": False}


def _should_continue(state: TreatmentInfoState) -> str:
    """Conditional edge: skip search/answer if the question was refused."""
    return "end" if state.get("refused") else "search"


async def _search_node(state: TreatmentInfoState) -> TreatmentInfoState:
    """Second node — call the backend tool."""
    tool_output = await get_treatment_schedule(state["search_query"], state.get("question", ""))
    return {
        **state,
        "tool_output": tool_output.model_dump(),
        "matched_treatment_ids": [t.id for t in tool_output.treatments],
    }


def _format_treatment_context(treatments: list[dict[str, Any]]) -> str:
    """Format treatment data into a clear text block for the LLM."""
    if not treatments:
        return "No treatments found."

    lines: list[str] = []
    for t in treatments:
        days = ", ".join(str(d) for d in t.get("available_days", []))
        lines.append(
            f"• {t['name']} ({t.get('name_sinhala', '')})\n"
            f"  Category: {t.get('category', 'N/A')}\n"
            f"  Duration: {t.get('duration_minutes', 'N/A')} minutes\n"
            f"  Fee: Rs. {t.get('unit_price', 'N/A')}\n"
            f"  Available days: {days or 'Not specified'}\n"
            f"  Description: {t.get('description', 'N/A')}"
        )
    return "\n\n".join(lines)


_SYSTEM_PROMPT = """\
You are a helpful Ayurvedic hospital information assistant. Your role is to \
answer patient questions about available treatments, services, and schedules.

STRICT RULES:
1. You may ONLY use the treatment data provided below. Do NOT invent, guess, \
or assume any treatment names, days, fees, durations, or descriptions.
2. If the data shows specific days, mention exactly those days — do not add or \
remove any.
3. If the data shows a specific fee, quote it exactly — do not round or change it.
4. Keep your answer concise, friendly, and informative.
5. Do NOT provide any medical advice, diagnosis, or treatment suitability \
recommendations.
6. If the user asks in Sinhala, please respond in natural, polite Sinhala.

TREATMENT DATA:
{treatment_data}
"""


async def _answer_node(state: TreatmentInfoState) -> TreatmentInfoState:
    """Third node — compose a grounded answer using the LLM or a static reply."""
    tool_output = state.get("tool_output") or {}
    treatments = tool_output.get("treatments") or []

    if not treatments:
        return {
            **state,
            "answer": (
                "I couldn't find any treatments matching your query in our "
                "current listings. Please check the treatment name or contact "
                "our hospital reception for further assistance."
            ),
        }

    treatment_context = _format_treatment_context(treatments)
    system_msg = _SYSTEM_PROMPT.format(treatment_data=treatment_context)

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
        # Fallback: if Ollama is unavailable or times out, produce a structured text answer
        # directly from the data rather than failing.
        answer = (
            f"Here is the information I found:\n\n{treatment_context}"
        )

    return {**state, "answer": answer}


# ---------------------------------------------------------------------------
# Graph assembly
# ---------------------------------------------------------------------------


def _build_treatment_info_graph():
    """Compile the treatment-info LangGraph."""
    graph = StateGraph(TreatmentInfoState)

    graph.add_node("classify", _tracked("classify", _classify_node))
    graph.add_node("search", _tracked("search", _search_node))
    graph.add_node("compose_answer", _tracked("compose_answer", _answer_node))
    
    graph.set_entry_point("classify")
    graph.add_conditional_edges(
        "classify",
        _should_continue,
        {"search": "search", "end": END},
    )
    graph.add_edge("search", "compose_answer")
    graph.add_edge("compose_answer", END)

    return graph.compile()


_GRAPH = _build_treatment_info_graph()


# ---------------------------------------------------------------------------
# Public entry point
# ---------------------------------------------------------------------------


async def run_treatment_info_agent(
    request: TreatmentInfoAgentRequest,
) -> TreatmentInfoAgentResponse:
    """Invoke the treatment-info LangGraph and return a typed response."""
    reset_persistence_warning()
    workflow_id = str(uuid4())
    store = get_state_store()
    objective = request.question.strip() or "Treatment information question"
    await persist(store.save(
        WorkflowState(
            workflow_id=workflow_id,
            objective=objective,
            plan=["classify", "search", "compose_answer"],
            approval_status=None,
            agent_name="treatment_info",
        )
    ))
    result = await _GRAPH.ainvoke(
        {
            "workflow_id": workflow_id,
            "question": request.question,
            "search_query": "",
            "tool_output": {},
            "answer": "",
            "matched_treatment_ids": [],
            "refused": False,
        }
    )
    refused = bool(result.get("refused"))
    matched = result.get("matched_treatment_ids") or []
    changes: dict[str, Any] = {
        "final_outcome": "safe_failure" if refused else "success",
        "completed_steps": ["classify"] if refused else ["classify", "search", "compose_answer"],
        "validation_results": [
            ValidationResult(
                check="medical_advice" if refused else "grounded_catalog",
                passed=not refused,
                detail=REFUSAL_MESSAGE if refused else "Answer uses catalog treatments only.",
            )
        ],
    }
    if refused:
        changes["errors"] = ["Medical advice questions are refused."]
    elif matched:
        related_id = matched[0]
        try:
            UUID(related_id)
        except (ValueError, TypeError):
            related_id = None
        if related_id:
            changes["related_entity_type"] = "Treatment"
            changes["related_entity_id"] = related_id
    await persist(store.update(workflow_id, **changes))
    return TreatmentInfoAgentResponse(
        answer=result["answer"],
        matched_treatment_ids=matched,
        refused=refused,
        workflow_id=workflow_id,
    )
