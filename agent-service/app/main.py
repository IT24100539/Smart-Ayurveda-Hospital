from fastapi import FastAPI, Header, HTTPException
from fastapi.responses import JSONResponse

from app.agents.treatment_info_agent import run_treatment_info_agent
from app.graph.coordinator import invoke_graph
from app.schemas import TreatmentInfoAgentRequest, TreatmentInfoAgentResponse
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


@app.post("/internal/agents/treatment-info")
async def treatment_info(
    request: TreatmentInfoAgentRequest,
    x_internal_secret: str | None = Header(default=None, alias="X-Internal-Secret"),
) -> TreatmentInfoAgentResponse:
    if x_internal_secret != settings.shared_secret:
        raise HTTPException(status_code=401, detail="Invalid internal secret.")
    return await run_treatment_info_agent(request)
