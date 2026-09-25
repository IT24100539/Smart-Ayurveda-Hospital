"""HTTP store mapping against the in-process Hospital.Api stand-in."""

from uuid import uuid4

from app.schemas import ApprovalStatus, ToolResult, ValidationResult, WorkflowState
from app.state_store import get_state_store


async def test_http_store_round_trip_maps_workflow_fields():
    store = get_state_store()
    workflow_id = str(uuid4())
    related_id = str(uuid4())
    saved = await store.save(WorkflowState(
        workflow_id=workflow_id,
        objective="Explain the shirodhara schedule.",
        plan=["classify"],
        agent_name="treatment_info",
        approval_status=None,
        related_entity_type="Treatment",
        related_entity_id=related_id,
        tool_results=[ToolResult(tool="search", succeeded=True, output={"count": 1})],
        validation_results=[ValidationResult(check="grounded_catalog", passed=True, detail="catalog")],
    ))
    updated = await store.update(
        saved.workflow_id,
        completed_steps=["classify", "search"],
        approval_status=ApprovalStatus.PENDING,
        final_outcome="success",
        errors=["none"],
    )
    store._cache.clear()
    loaded = await store.get(workflow_id)
    assert loaded is not None
    assert loaded.workflow_id == workflow_id
    assert loaded.agent_name == "treatment_info"
    assert loaded.objective == "Explain the shirodhara schedule."
    assert loaded.completed_steps == ["classify", "search"]
    assert loaded.tool_results[0].tool == "search"
    assert loaded.validation_results[0].passed is True
    assert loaded.errors == ["none"]
    assert loaded.approval_status == ApprovalStatus.PENDING
    assert loaded.final_outcome == "success"
    assert loaded.related_entity_type == "Treatment"
    assert loaded.related_entity_id == related_id
    assert updated.completed_steps == ["classify", "search"]
