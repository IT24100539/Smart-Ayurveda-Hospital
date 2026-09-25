"""One live persistence check per agent. Skips when Hospital.Api is not running."""

from uuid import uuid4

import httpx
import pytest

from app.schemas import WorkflowState
from app.settings import settings
from app.state_store import BackendApiWorkflowStateStore, get_state_store, set_state_store

pytestmark = pytest.mark.integration

_AGENTS = (
    ("scheduling_bed", "AdmissionRequest"),
    ("treatment_info", "Treatment"),
    ("feedback_support", "Feedback"),
    ("patient_info", "Patient"),
)


@pytest.fixture
async def live_store():
    base = settings.hospital_api_base_url.rstrip("/")
    key = settings.internal_service_key.get_secret_value() or "dev-internal-service-key"
    try:
        async with httpx.AsyncClient(timeout=2.0) as client:
            probe = await client.get(f"{base}/health")
            probe.raise_for_status()
    except (httpx.HTTPError, OSError) as exc:
        pytest.skip(f"Hospital.Api is not reachable at {base}: {exc}")
    previous = get_state_store()
    store = BackendApiWorkflowStateStore(base_url=base, api_key=key)
    set_state_store(store)
    yield store
    set_state_store(previous)


@pytest.mark.parametrize("agent_name,related_type", _AGENTS)
async def test_agent_workflow_round_trip(live_store, agent_name, related_type):
    workflow_id = str(uuid4())
    related_id = str(uuid4())
    saved = await live_store.save(WorkflowState(
        workflow_id=workflow_id,
        objective=f"{agent_name} durable workflow snapshot",
        plan=["start"],
        agent_name=agent_name,
        approval_status=None,
        related_entity_type=related_type,
        related_entity_id=related_id,
    ))
    updated = await live_store.update(
        saved.workflow_id,
        completed_steps=["start"],
        final_outcome="success",
        errors=[],
    )
    live_store._cache.clear()
    loaded = await live_store.get(workflow_id)
    assert loaded is not None
    assert loaded.agent_name == agent_name
    assert loaded.completed_steps == ["start"]
    assert updated.final_outcome == "success"
    assert loaded.related_entity_type == related_type
    assert loaded.related_entity_id == related_id
