"""Coordinator graph: routes a staff prompt to one specialist agent.

This service is internal-only. The public ASP.NET API is the only caller.
"""

from langgraph.graph import END, StateGraph

from app.agents.appointment_agent import run_appointment_agent
from app.agents.intake_agent import run_intake_agent
from app.agents.treatment_agent import run_treatment_agent
from app.graph.state import AgentState

# Feedback replies are not drafted here. That workflow is
# POST /internal/agents/feedback-support (feedback_support_agent).
KNOWN_AGENTS = {"intake", "appointment", "treatment", "coordinator"}


def route_node(state: AgentState) -> AgentState:
    requested = (state.get("agent") or "coordinator").lower()
    if requested in {"intake", "appointment", "treatment"}:
        route = requested
    else:
        text = state["prompt"].lower()
        if any(word in text for word in ("appoint", "slot", "schedule", "calendar")):
            route = "appointment"
        elif any(word in text for word in ("panchakarma", "abhyanga", "shirodhara", "treatment", "therapy")):
            route = "treatment"
        else:
            route = "intake"
    return {**state, "route": route, "metadata": {**state.get("metadata", {}), "route": route}}


def dispatch_node(state: AgentState) -> AgentState:
    route = state["route"]
    if route == "appointment":
        reply = run_appointment_agent(state["prompt"], state.get("context") or {})
    elif route == "treatment":
        reply = run_treatment_agent(state["prompt"], state.get("context") or {})
    else:
        reply = run_intake_agent(state["prompt"], state.get("context") or {})
    return {**state, "reply": reply}


def build_graph():
    graph = StateGraph(AgentState)
    graph.add_node("choose_agent", route_node)
    graph.add_node("run_agent", dispatch_node)
    graph.set_entry_point("choose_agent")
    graph.add_edge("choose_agent", "run_agent")
    graph.add_edge("run_agent", END)
    return graph.compile()


_GRAPH = build_graph()


async def invoke_graph(agent: str, prompt: str, context: dict) -> dict:
    if agent.lower() not in KNOWN_AGENTS:
        agent = "coordinator"
    normalized_context = {str(k): str(v) for k, v in (context or {}).items()}
    result = await _GRAPH.ainvoke(
        {
            "agent": agent,
            "prompt": prompt,
            "context": normalized_context,
            "route": "",
            "reply": "",
            "metadata": {},
        }
    )
    return {
        "agent": result["route"],
        "reply": result["reply"],
        "metadata": result.get("metadata") or {},
    }
