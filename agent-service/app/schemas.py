"""Shared Pydantic models for agent workflows.

Every agent in this service reads and writes the same `WorkflowState`, so a
plan produced by one member's agent stays legible to the coordinator and to
the ASP.NET API that ultimately persists it.
"""

from datetime import UTC, datetime
from enum import Enum
from typing import Any
from uuid import uuid4

from pydantic import BaseModel, Field


class ApprovalStatus(str, Enum):
    """Where a workflow sits in the human-in-the-loop review cycle.

    No Ayurvedic treatment, prescription, or bed assignment leaves this service
    as an accomplished fact: a vaidya approves it first.
    """

    PENDING = "pending"
    APPROVED = "approved"
    REJECTED = "rejected"
    REVISION = "revision"


class ToolResult(BaseModel):
    """Outcome of a single tool call, successful or not."""

    tool: str
    succeeded: bool
    output: dict[str, Any] = Field(default_factory=dict)
    error: str | None = None
    called_at: datetime = Field(default_factory=lambda: datetime.now(UTC))


class ValidationResult(BaseModel):
    """Outcome of one check run against a proposed step."""

    check: str
    passed: bool
    detail: str = ""


class WorkflowState(BaseModel):
    """The full state of one agent workflow.

    Treat instances as immutable snapshots: mutate through
    `WorkflowStateStore.update` so a swapped-in persistent store sees every
    change, rather than editing fields on a local copy.
    """

    workflow_id: str = Field(default_factory=lambda: str(uuid4()))
    objective: str
    plan: list[str] = Field(default_factory=list)
    completed_steps: list[str] = Field(default_factory=list)
    tool_results: list[ToolResult] = Field(default_factory=list)
    validation_results: list[ValidationResult] = Field(default_factory=list)
    errors: list[str] = Field(default_factory=list)
    approval_status: ApprovalStatus = ApprovalStatus.PENDING
    final_outcome: str | None = None

    @property
    def pending_steps(self) -> list[str]:
        return [step for step in self.plan if step not in self.completed_steps]

    @property
    def is_blocked(self) -> bool:
        """True when the workflow cannot advance without a human."""
        return bool(self.errors) or self.approval_status in {
            ApprovalStatus.REJECTED,
            ApprovalStatus.REVISION,
        }


# ---------------------------------------------------------------------------
# Treatment Information Agent schemas
# ---------------------------------------------------------------------------


class TreatmentScheduleItem(BaseModel):
    """One treatment summary as returned by the backend GET /api/treatments."""

    id: str
    name: str
    name_sinhala: str = Field(alias="nameSinhala", default="")
    description: str = ""
    description_sinhala: str = Field(alias="descriptionSinhala", default="")
    category: int | str = 0
    duration_minutes: int = Field(alias="durationMinutes", default=0)
    unit_price: float = Field(alias="unitPrice", default=0.0)
    is_active: bool = Field(alias="isActive", default=True)
    available_days: list[str] = Field(alias="availableDays", default_factory=list)

    model_config = {"populate_by_name": True}


class TreatmentScheduleToolOutput(BaseModel):
    """Typed wrapper for the tool's return value."""

    query: str
    treatments: list[TreatmentScheduleItem] = Field(default_factory=list)


class TreatmentInfoAgentRequest(BaseModel):
    """Patient-facing question about treatments, services, or schedules."""

    question: str


class TreatmentInfoAgentResponse(BaseModel):
    """Grounded answer to a treatment-information question."""

    answer: str
    matched_treatment_ids: list[str] = Field(default_factory=list)
    refused: bool = False
