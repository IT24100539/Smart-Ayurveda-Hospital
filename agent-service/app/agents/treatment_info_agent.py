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

import httpx
from langchain_core.messages import HumanMessage, SystemMessage
from langchain_ollama import ChatOllama
from langgraph import graph
from langgraph.graph import END, StateGraph

from app.schemas import (
    TreatmentInfoAgentRequest,
    TreatmentInfoAgentResponse,
    TreatmentScheduleItem,
    TreatmentScheduleToolOutput,
)
from app.settings import settings

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
    "a an the is are was were what when where which who whom how do does "
    "did will would shall can could may might about on in at to for of and "
    "or but with from by this that these those it its i me my we our you "
    "your they their there here please tell show give list "
    "available availability schedule scheduled cost cost price much service treatment".split()
)


def _extract_search_query(question: str) -> str:
    """Derive a concise search keyword from a natural-language question.

    Strips punctuation and common stop words, returning the remaining
    meaningful terms that the backend ``Name`` filter can match against.
    """
    words = re.sub(r"[^\w\s]", "", question).split()
    meaningful = [w for w in words if w.lower() not in _STOPWORDS]
    return " ".join(meaningful[:5]) if meaningful else question.strip()[:50]


# ---------------------------------------------------------------------------
# Tool: get_treatment_schedule
# ---------------------------------------------------------------------------


async def get_treatment_schedule(query: str) -> TreatmentScheduleToolOutput:
    """Call the backend ``GET /api/treatments?name={query}`` endpoint.

    Returns a typed :class:`TreatmentScheduleToolOutput` with matched
    treatments.  On any HTTP error or when no results are found the
    treatments list is empty — the function never raises.
    """
    try:
        async with httpx.AsyncClient(timeout=10.0, verify=False) as client:
            url = f"{settings.backend_base_url}/api/treatments"
            resp = await client.get(url, params={"name": query})
            resp.raise_for_status()
            data = resp.json()

            items_raw: list[dict[str, Any]] = data.get("items") or []

            treatments = [
                TreatmentScheduleItem.model_validate(item) for item in items_raw
            ]
    except Exception:
        treatments = []
    
    return TreatmentScheduleToolOutput(query=query, treatments=treatments)


# ---------------------------------------------------------------------------
# LangGraph state & nodes
# ---------------------------------------------------------------------------


class TreatmentInfoState(TypedDict):
    question: str
    search_query: str
    tool_output: dict[str, Any]
    answer: str
    matched_treatment_ids: list[str]
    refused: bool


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
    tool_output = await get_treatment_schedule(state["search_query"])
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
        llm = ChatOllama(
            base_url=settings.ollama_base_url,
            model=settings.ollama_model,
            temperature=0.1,
        )
        response = await llm.ainvoke([
            SystemMessage(content=system_msg),
            HumanMessage(content=state["question"]),
        ])
        answer = response.content
    except Exception:
        # Fallback: if Ollama is unavailable, produce a structured text answer
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

    graph.add_node("classify", _classify_node)
    graph.add_node("search", _search_node)
    graph.add_node("compose_answer", _answer_node)
    
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
    result = await _GRAPH.ainvoke(
        {
            "question": request.question,
            "search_query": "",
            "tool_output": {},
            "answer": "",
            "matched_treatment_ids": [],
            "refused": False,
        }
    )
    return TreatmentInfoAgentResponse(
        answer=result["answer"],
        matched_treatment_ids=result.get("matched_treatment_ids") or [],
        refused=result.get("refused", False),
    )
