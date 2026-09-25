from fastapi import Depends, FastAPI, Header, HTTPException
from fastapi.responses import JSONResponse

from app.agents.feedback_support_agent import (
    FeedbackAgentRequest,
    FeedbackAgentResponse,
    run_feedback_support,
)
from app.agents.scheduling_bed_agent import run_scheduling_bed_agent
from app.agents.treatment_info_agent import run_treatment_info_agent
from app.graph.coordinator import CoordinatorRequest, CoordinatorResponse, coordinate, invoke_graph
from app.schemas import (
    SchedulingAgentRequest,
    SchedulingAgentResponse,
    TreatmentInfoAgentRequest,
    TreatmentInfoAgentResponse,
)
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


async def require_internal_secret(
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> None:
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")


@app.post("/internal/agents/scheduling-bed", response_model=SchedulingAgentResponse,
          dependencies=[Depends(require_internal_secret)])
async def scheduling_bed(request: SchedulingAgentRequest) -> SchedulingAgentResponse:
    return await run_scheduling_bed_agent(request)


@app.post("/internal/agents/coordinate", response_model=CoordinatorResponse,
          dependencies=[Depends(require_internal_secret)])
async def coordinate_workflow(request: CoordinatorRequest) -> CoordinatorResponse:
    """Single entry for starting an agentic workflow. Specialists are not called directly."""
    return await coordinate(request)


@app.post("/v1/invoke", dependencies=[Depends(require_internal_secret)])
async def invoke(payload: dict) -> JSONResponse:
    agent = str(payload.get("agent") or payload.get("Agent") or "coordinator")
    prompt = str(payload.get("prompt") or payload.get("Prompt") or "").strip()
    context = payload.get("context") or payload.get("Context") or {}
    if not prompt:
        raise HTTPException(status_code=400, detail="Prompt is required.")

    result = await invoke_graph(agent=agent, prompt=prompt, context=context)
    return JSONResponse(result)


@app.post("/internal/agents/treatment-info")
async def treatment_info(
    request: TreatmentInfoAgentRequest,
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> TreatmentInfoAgentResponse:
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")
    return await run_treatment_info_agent(request)


@app.post("/internal/agents/feedback-support", response_model=FeedbackAgentResponse)
async def feedback_support(
    payload: FeedbackAgentRequest,
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> FeedbackAgentResponse:
    """Run the feedback-support graph. The agent returns a draft; it does not publish one."""
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")
    return await run_feedback_support(payload)