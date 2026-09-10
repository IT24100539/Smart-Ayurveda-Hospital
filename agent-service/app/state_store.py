"""Persistence abstraction for `WorkflowState`.

TODO(Member 3/4): agent state MUST end up in Postgres, not in this Python
process. The assignment requires Postgres as the system of record, and this
service is a stateless internal worker that may be restarted or run more than
once -- anything held in `InMemoryWorkflowStateStore` is lost on restart and is
invisible to a second worker.

The replacement is deliberately a one-file change: add a
`BackendApiWorkflowStateStore(WorkflowStateStore)` here that persists through
the ASP.NET Core API (which owns the EF Core context and the Postgres
connection), then point `get_state_store` at it. Nothing else in the service
imports a concrete store, so no agent, graph, or route needs to change.

Do not open a direct Postgres connection from this service: `backend/` is the
only component that talks to the database.

The interface is async purely so that swap stays signature-compatible -- the
backend-backed implementation will be doing HTTP I/O.
"""

from abc import ABC, abstractmethod
from asyncio import Lock
from typing import Any

from app.schemas import WorkflowState


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


_store: WorkflowStateStore = InMemoryWorkflowStateStore()


def get_state_store() -> WorkflowStateStore:
    """The single seam for swapping in the Postgres-backed store."""
    return _store


def set_state_store(store: WorkflowStateStore) -> None:
    """Override the active store. For tests and for wiring at startup."""
    global _store
    _store = store
