from fastapi import Depends, FastAPI, Header, HTTPException
from fastapi.responses import JSONResponse

from app.graph.coordinator import invoke_graph
from app.agents.scheduling_bed_agent import run_scheduling_bed_agent
from app.schemas import SchedulingAgentRequest, SchedulingAgentResponse
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


@app.post("/v1/invoke", dependencies=[Depends(require_internal_secret)])
async def invoke(payload: dict) -> JSONResponse:
    agent = str(payload.get("agent") or payload.get("Agent") or "coordinator")
    prompt = str(payload.get("prompt") or payload.get("Prompt") or "").strip()
    context = payload.get("context") or payload.get("Context") or {}
    if not prompt:
        raise HTTPException(status_code=400, detail="Prompt is required.")

    result = await invoke_graph(agent=agent, prompt=prompt, context=context)
    return JSONResponse(result)
