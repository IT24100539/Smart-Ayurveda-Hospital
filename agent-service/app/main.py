from fastapi import FastAPI, Header, HTTPException
from fastapi.responses import JSONResponse

from app.agents.feedback_support_agent import (
    FeedbackAgentRequest,
    FeedbackAgentResponse,
    run_feedback_support,
)
from app.graph.coordinator import invoke_graph
from app.settings import settings

app = FastAPI(
    title="Smart Ayurveda Agent Service",
    version="0.1.0",
    docs_url=None if settings.is_production else "/docs",
    redoc_url=None,
)


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok", "service": "agent-service"}


@app.post("/v1/invoke")
async def invoke(
    payload: dict,
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> JSONResponse:
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")

    agent = str(payload.get("agent") or payload.get("Agent") or "coordinator")
    prompt = str(payload.get("prompt") or payload.get("Prompt") or "").strip()
    context = payload.get("context") or payload.get("Context") or {}
    if not prompt:
        raise HTTPException(status_code=400, detail="Prompt is required.")

    result = await invoke_graph(agent=agent, prompt=prompt, context=context)
    return JSONResponse(result)


@app.post("/internal/agents/feedback-support", response_model=FeedbackAgentResponse)
async def feedback_support(
    payload: FeedbackAgentRequest,
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> FeedbackAgentResponse:
    """Run the feedback-support graph. The agent returns a draft; it does not publish one."""
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")
    return await run_feedback_support(payload)
