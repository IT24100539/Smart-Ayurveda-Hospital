"""Unit tests talk to a fake Hospital.Api so workflow persistence stays in-process."""

import json

import httpx
import pytest

from app.state_store import BackendApiWorkflowStateStore, get_state_store, set_state_store


def row_from_write(body: dict, workflow_id: str) -> dict:
    return {
        "id": workflow_id,
        "agentName": body["agentName"],
        "objectiveText": body["objectiveText"],
        "plan": body["planJson"],
        "completedSteps": body["completedStepsJson"],
        "toolResults": body["toolResultsJson"],
        "validationResults": body["validationResultsJson"],
        "errors": body.get("errorsJson"),
        "approvalStatus": body["approvalStatus"],
        "finalOutcome": body["finalOutcome"],
        "relatedEntityType": body.get("relatedEntityType"),
        "relatedEntityId": body.get("relatedEntityId"),
        "createdAt": "2026-09-25T00:00:00Z",
        "updatedAt": "2026-09-25T00:00:00Z",
    }


class WorkflowApiMemory(httpx.AsyncBaseTransport):
    def __init__(self) -> None:
        self.rows: dict[str, dict] = {}

    async def handle_async_request(self, request: httpx.Request) -> httpx.Response:
        path = request.url.path
        body = json.loads(request.content) if request.content else {}
        if request.method == "POST" and path == "/api/internal/workflow-executions":
            workflow_id = body["id"]
            self.rows[workflow_id] = row_from_write(body, workflow_id)
            return httpx.Response(200, json=self.rows[workflow_id])
        prefix = "/api/internal/workflow-executions/"
        if path.startswith(prefix):
            workflow_id = path[len(prefix):]
            if request.method == "GET":
                row = self.rows.get(workflow_id)
                if row is None:
                    return httpx.Response(404, json={"detail": "missing"})
                return httpx.Response(200, json=row)
            if request.method == "PATCH":
                if workflow_id not in self.rows:
                    return httpx.Response(404, json={"detail": "missing"})
                self.rows[workflow_id] = row_from_write(body, workflow_id)
                return httpx.Response(200, json=self.rows[workflow_id])
        return httpx.Response(404, json={"detail": f"{request.method} {path}"})


@pytest.fixture(autouse=True)
def workflow_http_store():
    previous = get_state_store()
    client = httpx.AsyncClient(transport=WorkflowApiMemory(), base_url="http://hospital.test")
    set_state_store(BackendApiWorkflowStateStore(base_url="http://hospital.test", api_key="test-key", client=client))
    yield
    set_state_store(previous)
