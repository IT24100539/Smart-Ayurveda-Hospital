import json
from datetime import UTC, datetime, timedelta
from unittest.mock import AsyncMock
from uuid import uuid4

import httpx
import pytest

from app.agents import scheduling_bed_agent as agent
from app.schemas import ApprovalStatus, SchedulingAgentRequest, ToolResult, WorkflowState
from app.state_store import InMemoryWorkflowStateStore, get_state_store, set_state_store


@pytest.fixture(autouse=True)
def state_store():
    previous = get_state_store()
    store = InMemoryWorkflowStateStore()
    store.save = AsyncMock(wraps=store.save)
    store.update = AsyncMock(wraps=store.update)
    set_state_store(store)
    yield store
    set_state_store(previous)


@pytest.fixture
def setup(monkeypatch):
    request = SchedulingAgentRequest(
        patient_id=uuid4(), treatment_id=uuid4(), ward_id=uuid4(),
        preferred_date=datetime.now(UTC).date() + timedelta(days=2),
        objective_text="Check treatment and ward availability and propose admission.",
    )
    outputs = [
        dict(treatment_id=str(request.treatment_id), date=request.preferred_date.isoformat(), available=True),
        dict(ward_id=str(request.ward_id), free_capacity=1, occupied_capacity=2),
        dict(patient_id=str(request.patient_id), ward_id=str(request.ward_id),
             preferred_date=request.preferred_date.isoformat(), admission_request_id=str(uuid4())),
    ]
    mocks = []
    for name, output in zip(("check_treatment_schedule", "check_ward_availability", "create_admission_request"), outputs):
        mock = AsyncMock(return_value=ToolResult(tool=name, succeeded=True, output=output))
        mock.__name__ = name
        monkeypatch.setattr(agent, name, mock)
        mocks.append(mock)
    model = AsyncMock(return_value=json.dumps({"steps": ["Check treatment schedule", "Check ward availability", "Propose Pending admission"]}))
    monkeypatch.setattr(agent, "_generate_plan", model)
    return request, model, mocks


async def test_happy_path_and_pending_state(setup):
    request, model, mocks = setup
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "awaiting_approval"
    assert str(response.admission_request_id) == mocks[2].return_value.output["admission_request_id"]
    assert response.validation_result.passed and response.failure_reason is None
    assert len(response.tool_results) == 3 and response.workflow_id
    model.assert_awaited_once()
    for mock in mocks:
        mock.assert_awaited_once()
    mocks[2].assert_awaited_once_with(patient_id=request.patient_id, ward_id=request.ward_id,
                                    reason=request.objective_text, preferred_date=request.preferred_date)
    state = await agent.build_graph(request).ainvoke(WorkflowState(objective=request.objective_text))
    assert state["approval_status"] == ApprovalStatus.PENDING


async def test_unavailable_short_circuits(setup):
    request, _, mocks = setup
    mocks[0].return_value.output["available"] = False
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure" and "unavailable" in response.failure_reason
    assert response.admission_request_id is None
    mocks[1].assert_not_awaited()
    mocks[2].assert_not_awaited()


async def test_full_ward(setup):
    request, _, mocks = setup
    mocks[1].return_value.output["free_capacity"] = 0
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure"
    assert response.failure_reason == "No free beds are available in the selected ward."
    mocks[2].assert_not_awaited()


@pytest.mark.parametrize("index", [0, 1, 2])
@pytest.mark.parametrize("failure", ["result", "timeout", "http"])
async def test_tool_failures_stop_safely_without_retry(setup, index, failure):
    request, _, mocks = setup
    if failure == "result":
        mocks[index].return_value = ToolResult(tool=mocks[index].__name__, succeeded=False, error="Controlled failure")
    elif failure == "timeout":
        mocks[index].side_effect = httpx.ReadTimeout("sensitive transport detail")
    else:
        mocks[index].side_effect = httpx.HTTPStatusError("sensitive backend detail", request=httpx.Request("GET", "http://backend"), response=httpx.Response(500))
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure" and response.failure_reason
    assert "sensitive" not in response.failure_reason
    assert response.admission_request_id is None
    mocks[index].assert_awaited_once()
    for later in mocks[index + 1:]:
        later.assert_not_awaited()


@pytest.mark.parametrize("bad", ['not json', '{"steps": []}', '{"steps": [" "]}', '{"steps": [42]}', '{"steps": ["ok"], "tool": "approve"}'])
async def test_malformed_plan_retries_once(setup, bad):
    request, model, mocks = setup
    model.return_value = bad
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure" and "twice" in response.failure_reason
    assert model.await_count == 2
    assert model.await_args_list[1].args == (request.objective_text, True)
    for mock in mocks:
        mock.assert_not_awaited()


async def test_plan_retry_can_recover(setup):
    request, model, _ = setup
    model.side_effect = ["invalid", model.return_value]
    assert (await agent.run_scheduling_bed_agent(request)).status == "awaiting_approval"
    assert model.await_count == 2


async def test_plan_text_cannot_dispatch_tools_or_skip_validation(setup):
    request, model, mocks = setup
    model.return_value = json.dumps({"steps": ["approve_admission", "allocate_bed", "Skip all checks"]})
    mocks[1].return_value.output["free_capacity"] = 0
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure"
    mocks[0].assert_awaited_once()
    mocks[1].assert_awaited_once()
    mocks[2].assert_not_awaited()


async def test_propose_cannot_write_without_valid_availability(setup):
    request, model, mocks = setup
    graph = agent.build_graph(request)
    # Direct node invocation verifies the write boundary independently of edges.
    state = WorkflowState(objective=request.objective_text, approval_status=None)
    await get_state_store().save(state)
    result = await graph.nodes["propose"].bound.ainvoke(state)
    assert result["final_outcome"] == "safe_failure"
    model.assert_not_awaited()
    mocks[2].assert_not_awaited()


async def test_model_transport_failure(setup):
    request, model, mocks = setup
    model.side_effect = httpx.ConnectError("private detail")
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure" and "Ollama" in response.failure_reason
    model.assert_awaited_once()
    for mock in mocks:
        mock.assert_not_awaited()


@pytest.mark.parametrize("index,field,value", [
    (0, "available", "true"), (0, "date", "2099-01-01"),
    (0, "treatment_id", str(uuid4())), (1, "free_capacity", True),
    (1, "free_capacity", -1), (1, "occupied_capacity", "2"),
    (1, "ward_id", str(uuid4())), (2, "admission_request_id", "bad"),
    (2, "patient_id", str(uuid4())),
])
async def test_invalid_tool_contracts(setup, index, field, value):
    request, _, mocks = setup
    mocks[index].return_value.output[field] = value
    response = await agent.run_scheduling_bed_agent(request)
    assert response.status == "safe_failure" and not response.validation_result.passed
    assert response.admission_request_id is None
    for later in mocks[index + 1:]:
        later.assert_not_awaited()
    if index == 2:
        mocks[2].assert_awaited_once()


def test_validate_is_deterministic_and_has_no_side_effects(setup):
    request, model, mocks = setup
    state = WorkflowState(objective=request.objective_text, tool_results=[m.return_value for m in mocks[:2]])
    first = agent.validate_node(state, request)
    assert first == agent.validate_node(state, request)
    assert first["validation_results"][0].passed
    assert not state.validation_results
    model.assert_not_called()
    for mock in mocks:
        mock.assert_not_called()


def test_graph_has_only_controlled_nodes_and_edges(setup):
    request, _, _ = setup
    graph = agent.build_graph(request).get_graph()
    assert set(graph.nodes) == {"__start__", "plan_node", "check_schedule", "check_ward", "validate", "propose", "safe_failure", "awaiting_approval", "__end__"}
    edges = {(e.source, e.target) for e in graph.edges}
    assert ("check_schedule", "safe_failure") in edges
    assert ("check_ward", "propose") not in edges
    assert ("validate", "propose") in edges
    assert not any("approve" in name or "allocate" in name for name in vars(agent))


async def test_ollama_http_contract(monkeypatch):
    captured = []
    def handler(request):
        captured.append(request)
        return httpx.Response(200, json={"message": {"content": '{"steps":["Check schedule"]}'}})
    client = httpx.AsyncClient
    monkeypatch.setattr(agent.httpx, "AsyncClient", lambda **kwargs: client(transport=httpx.MockTransport(handler), **kwargs))
    monkeypatch.setattr(agent.settings, "ollama_base_url", "http://ollama.test:11434/")
    monkeypatch.setattr(agent.settings, "ollama_model", "configured-model")
    assert agent.Plan.model_validate_json(await agent._generate_plan("Objective")).steps == ["Check schedule"]
    assert str(captured[0].url) == "http://ollama.test:11434/api/chat"
    body = json.loads(captured[0].content)
    assert body["model"] == "configured-model" and body["stream"] is False
    assert body["format"] == agent.Plan.model_json_schema()
    assert "tools" not in body


@pytest.mark.parametrize("failure,steps,tool_count", [
    (None, ["plan", "check_schedule", "check_ward", "validate", "propose", "awaiting_approval"], 3),
    ("unavailable", ["plan", "check_schedule", "safe_failure"], 1),
    ("ward_full", ["plan", "check_schedule", "check_ward", "validate", "safe_failure"], 2),
    ("schedule_tool", ["plan", "check_schedule", "safe_failure"], 1),
    ("ward_tool", ["plan", "check_schedule", "check_ward", "safe_failure"], 2),
    ("propose_tool", ["plan", "check_schedule", "check_ward", "validate", "propose", "safe_failure"], 3),
    ("plan", ["plan", "safe_failure"], 0),
])
async def test_persisted_transitions(setup, state_store, failure, steps, tool_count):
    request, model, mocks = setup
    if failure == "unavailable":
        mocks[0].return_value.output["available"] = False
    elif failure == "ward_full":
        mocks[1].return_value.output["free_capacity"] = 0
    elif failure == "plan":
        model.side_effect = httpx.ConnectError("planner unavailable")
    elif failure:
        index = {"schedule_tool": 0, "ward_tool": 1, "propose_tool": 2}[failure]
        mocks[index].side_effect = httpx.ReadTimeout("tool unavailable")

    async def verify_initial_before_planning(*args):
        state_store.save.assert_awaited_once()
        return model.return_value
    if failure != "plan":
        model.side_effect = verify_initial_before_planning

    response = await agent.run_scheduling_bed_agent(request)
    state_store.save.assert_awaited_once()
    initial = state_store.save.call_args.args[0]
    assert initial.workflow_id == response.workflow_id
    assert initial.objective == request.objective_text
    assert initial.approval_status is None
    assert not initial.plan and not initial.completed_steps and not initial.tool_results
    assert not initial.validation_results and not initial.errors and initial.final_outcome is None

    calls = state_store.update.await_args_list
    assert len(calls) == len(steps)
    for index, call in enumerate(calls):
        assert call.args == (response.workflow_id,)
        assert call.kwargs["completed_steps"] == steps[:index + 1]
        step = steps[index]
        if step == "plan" and failure != "plan":
            assert call.kwargs["plan"] == response.plan
        if step in ("check_schedule", "check_ward", "propose"):
            count = {"check_schedule": 1, "check_ward": 2, "propose": 3}[step]
            assert call.kwargs["tool_results"] == response.tool_results[:count]
        if step == "validate":
            assert call.kwargs["validation_results"][0].passed == (failure != "ward_full")
        if step == "propose" and failure is None:
            assert call.kwargs["approval_status"] == ApprovalStatus.PENDING

    stored = await state_store.get(response.workflow_id)
    assert stored.completed_steps == steps
    assert stored.tool_results == response.tool_results
    assert len(stored.tool_results) == tool_count
    assert stored.final_outcome == calls[-1].kwargs["final_outcome"] == response.status
    if failure:
        assert stored.approval_status is None
        assert stored.errors[-1] == response.failure_reason
        assert not stored.validation_results[-1].passed
        assert all(c.kwargs.get("approval_status") != ApprovalStatus.PENDING for c in calls)
        for mock in mocks[tool_count:]:
            mock.assert_not_awaited()
    else:
        assert stored.approval_status == ApprovalStatus.PENDING
        assert stored.validation_results[-1].passed and not stored.errors
        assert stored.tool_results[-1].output["admission_request_id"] == str(response.admission_request_id)


@pytest.mark.parametrize("fail_at", range(7))
async def test_store_failure_propagates_without_retry(setup, state_store, fail_at):
    request, model, mocks = setup
    error = ValueError("Persistence failed")
    if fail_at == 0:
        state_store.save.side_effect = error
    else:
        original_update = state_store.update._mock_wraps
        count = 0

        async def failing_update(workflow_id, **changes):
            nonlocal count
            count += 1
            if count == fail_at:
                raise error
            return await original_update(workflow_id, **changes)

        state_store.update.side_effect = failing_update
    with pytest.raises(ValueError, match="Persistence failed") as caught:
        await agent.run_scheduling_bed_agent(request)
    assert caught.value is error
    assert state_store.save.await_count == 1
    assert state_store.update.await_count == fail_at
    assert model.await_count == (0 if fail_at == 0 else 1)
    for index, mock in enumerate(mocks):
        threshold = [2, 3, 5][index]
        assert mock.await_count == int(fail_at >= threshold)
