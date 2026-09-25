"""Persistence abstraction for `WorkflowState`.

Postgres is the system of record. This service does not open a database
connection. `BackendApiWorkflowStateStore` writes each snapshot through
Hospital.Api, which owns the EF Core context. `InMemoryWorkflowStateStore`
remains for tests that replace the store explicitly.
"""

from abc import ABC, abstractmethod
from asyncio import Lock
from typing import Any

import httpx

from app.schemas import ApprovalStatus, WorkflowState
from app.settings import settings


class StateNotFoundError(KeyError):
    """Raised when a workflow id is not present in the store."""

    def __init__(self, workflow_id: str) -> None:
        super().__init__(workflow_id)
        self.workflow_id = workflow_id

    def __str__(self) -> str:
        return f"No workflow state for id '{self.workflow_id}'."


class WorkflowStateStore(ABC):
    """Save/get/update contract every store implementation honours."""

    @abstractmethod
    async def save(self, state: WorkflowState) -> WorkflowState:
        """Insert or overwrite the state for `state.workflow_id`."""

    @abstractmethod
    async def get(self, workflow_id: str) -> WorkflowState | None:
        """Return the stored state, or None when the id is unknown."""

    @abstractmethod
    async def update(self, workflow_id: str, **changes: Any) -> WorkflowState:
        """Apply partial field changes and return the new state.

        Raises `StateNotFoundError` if the workflow does not exist, and
        `pydantic.ValidationError` if a change violates the schema.
        """


class InMemoryWorkflowStateStore(WorkflowStateStore):
    """Development-only store. See the module TODO before relying on this."""

    def __init__(self) -> None:
        self._states: dict[str, WorkflowState] = {}
        # `update` is a read-modify-write, so concurrent graph nodes touching
        # one workflow would otherwise drop each other's changes.
        self._lock = Lock()

    async def save(self, state: WorkflowState) -> WorkflowState:
        async with self._lock:
            self._states[state.workflow_id] = state
            return state

    async def get(self, workflow_id: str) -> WorkflowState | None:
        async with self._lock:
            return self._states.get(workflow_id)

    async def update(self, workflow_id: str, **changes: Any) -> WorkflowState:
        async with self._lock:
            current = self._states.get(workflow_id)
            if current is None:
                raise StateNotFoundError(workflow_id)
            updated = current.model_copy(update=changes)
            # Re-validate: model_copy trusts its input, so an invalid value
            # would otherwise be stored and only fail much later.
            updated = WorkflowState.model_validate(updated.model_dump())
            self._states[workflow_id] = updated
            return updated


_APPROVAL_TO_API = {
    ApprovalStatus.PENDING: "Pending",
    ApprovalStatus.APPROVED: "Approved",
    ApprovalStatus.REJECTED: "Rejected",
    ApprovalStatus.REVISION: "RevisionRequested",
}
_APPROVAL_FROM_API = {value: key for key, value in _APPROVAL_TO_API.items()}


def _approval_to_api(status: ApprovalStatus | None) -> str:
    if status is None:
        return "NotRequired"
    return _APPROVAL_TO_API[status]


def _approval_from_api(value: str | None) -> ApprovalStatus | None:
    if value in (None, "NotRequired"):
        return None
    return _APPROVAL_FROM_API[value]


def _outcome_to_api(value: str | None) -> str:
    if value is None or not str(value).strip():
        return "InProgress"
    token = str(value).strip().lower().replace(" ", "_").replace("-", "_")
    if token in {"safe_failure", "failure", "failed"}:
        return "SafeFailure"
    if token in {"success", "awaiting_approval", "awaiting_review", "completed"}:
        return "Success"
    return "SafeFailure"


def _outcome_from_api(value: str | None, agent_name: str) -> str | None:
    if value in (None, "InProgress"):
        return None
    if value == "SafeFailure":
        return "safe_failure"
    if agent_name == "scheduling_bed":
        return "awaiting_approval"
    if agent_name == "feedback_support":
        return "awaiting_review"
    return "success"


def _payload(state: WorkflowState, *, include_id: bool) -> dict[str, Any]:
    body: dict[str, Any] = {
        "agentName": state.agent_name or "unspecified",
        "objectiveText": state.objective,
        "planJson": state.plan,
        "completedStepsJson": state.completed_steps,
        "toolResultsJson": [item.model_dump(mode="json") for item in state.tool_results],
        "validationResultsJson": [item.model_dump(mode="json") for item in state.validation_results],
        "errorsJson": state.errors,
        "approvalStatus": _approval_to_api(state.approval_status),
        "finalOutcome": _outcome_to_api(state.final_outcome),
        "relatedEntityType": state.related_entity_type,
        "relatedEntityId": state.related_entity_id,
    }
    if include_id:
        body["id"] = state.workflow_id
    return body


def _state_from_body(body: dict[str, Any]) -> WorkflowState:
    agent_name = str(body.get("agentName") or "unspecified")
    errors = body.get("errors")
    return WorkflowState.model_validate(
        {
            "workflow_id": str(body["id"]),
            "objective": body.get("objectiveText") or "",
            "plan": body.get("plan") or [],
            "completed_steps": body.get("completedSteps") or [],
            "tool_results": body.get("toolResults") or [],
            "validation_results": body.get("validationResults") or [],
            "errors": [] if errors is None else errors,
            "approval_status": _approval_from_api(body.get("approvalStatus")),
            "final_outcome": _outcome_from_api(body.get("finalOutcome"), agent_name),
            "agent_name": agent_name,
            "related_entity_type": body.get("relatedEntityType"),
            "related_entity_id": None if body.get("relatedEntityId") is None else str(body["relatedEntityId"]),
        }
    )


class BackendApiWorkflowStateStore(WorkflowStateStore):
    """Persists workflow snapshots through Hospital.Api internal endpoints."""

    def __init__(
        self,
        base_url: str | None = None,
        api_key: str | None = None,
        client: httpx.AsyncClient | None = None,
    ) -> None:
        self._base_url = (base_url or settings.hospital_api_base_url).rstrip("/")
        key = settings.internal_service_key.get_secret_value() if api_key is None else api_key
        self._headers = {"X-Internal-Service-Key": key}
        self._client = client
        self._lock = Lock()
        self._cache: dict[str, WorkflowState] = {}

    async def save(self, state: WorkflowState) -> WorkflowState:
        async with self._lock:
            response = await self._request("POST", "/api/internal/workflow-executions", _payload(state, include_id=True))
            if response.status_code >= 400:
                response.raise_for_status()
            self._cache[state.workflow_id] = state
            return state

    async def get(self, workflow_id: str) -> WorkflowState | None:
        async with self._lock:
            cached = self._cache.get(workflow_id)
            if cached is not None:
                return cached
            response = await self._request("GET", f"/api/internal/workflow-executions/{workflow_id}")
            if response.status_code == 404:
                return None
            response.raise_for_status()
            state = _state_from_body(response.json())
            self._cache[workflow_id] = state
            return state

    async def update(self, workflow_id: str, **changes: Any) -> WorkflowState:
        async with self._lock:
            current = self._cache.get(workflow_id)
            if current is None:
                response = await self._request("GET", f"/api/internal/workflow-executions/{workflow_id}")
                if response.status_code == 404:
                    raise StateNotFoundError(workflow_id)
                response.raise_for_status()
                current = _state_from_body(response.json())
            updated = current.model_copy(update=changes)
            updated = WorkflowState.model_validate(updated.model_dump())
            response = await self._request(
                "PATCH",
                f"/api/internal/workflow-executions/{workflow_id}",
                _payload(updated, include_id=False),
            )
            if response.status_code == 404:
                raise StateNotFoundError(workflow_id)
            response.raise_for_status()
            self._cache[workflow_id] = updated
            return updated

    async def _request(self, method: str, path: str, json: dict[str, Any] | None = None) -> httpx.Response:
        if self._client is not None:
            return await self._client.request(method, f"{self._base_url}{path}", json=json, headers=self._headers)
        async with httpx.AsyncClient(timeout=httpx.Timeout(10.0)) as client:
            return await client.request(method, f"{self._base_url}{path}", json=json, headers=self._headers)


def _default_store() -> WorkflowStateStore:
    return BackendApiWorkflowStateStore()


_store: WorkflowStateStore = _default_store()


def get_state_store() -> WorkflowStateStore:
    """The active store. The default writes through Hospital.Api."""
    return _store


def set_state_store(store: WorkflowStateStore) -> None:
    """Override the active store. For tests and for wiring at startup."""
    global _store
    _store = store
