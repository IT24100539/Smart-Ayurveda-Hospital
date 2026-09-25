"""Shared Pydantic models for agent workflows.

Every agent in this service reads and writes the same `WorkflowState`, so a
plan produced by one member's agent stays legible to the coordinator and to
the ASP.NET API that ultimately persists it.
"""

import logging
import re
from datetime import UTC, date, datetime
from enum import Enum
from typing import Any, Literal
from uuid import UUID, uuid4

from pydantic import BaseModel, Field, ModelWrapValidatorHandler, ValidationError, field_validator, model_validator


logger = logging.getLogger(__name__)

# Deliberately small, auditable heuristic; this is not a security boundary for tools.
_INJECTION_PATTERNS = (
    r"\bignore\b.{0,40}\b(previous|prior|above)\s+instructions\b",
    r"\b(skip|bypass|disable|avoid)\b.{0,40}\bvalidation\b",
    r"\b(skip|bypass|disable|avoid)\b.{0,40}\bapproval\b",
    r"\b(without|no)\s+(human\s+)?approval\b",
    r"\b(outside|bypass|ignore)\b.{0,40}\b(allowlist|allow\s+list|allowed\s+tools)\b",
    r"\btools?\b.{0,40}\bnot\s+(?:on|in)\b.{0,20}\b(allowlist|allow\s+list)\b",
    r"\b(unapproved|unauthorized|unlisted|non[- ]allowlisted)\s+tools?\b",
    r"[\{,]\s*[\"'](?:tool(?:_results?|_calls?)?|function_call|role|approval_status|admission_request_id|succeeded)[\"']\s*:",
    r"[\{,]\s*[\"']status[\"']\s*:\s*[\"'](?:approved|success|bed_allocated)[\"']",
    r"(?:<\s*/?\s*tool[_ -]?results?\b|\btool[_ -]?results?\s*:)",
)


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


class SchedulingAgentRequest(BaseModel):
    """Validated entry contract; construct this before any scheduling work.

    Preferred dates use the service's UTC calendar day. Only a date object or
    an ISO YYYY-MM-DD string is accepted, never timestamps or epoch numbers.
    """

    patient_id: UUID
    treatment_id: UUID
    ward_id: UUID
    objective_text: str
    preferred_date: date

    @model_validator(mode="wrap")
    @classmethod
    def log_refusal(cls, value: Any, handler: ModelWrapValidatorHandler) -> "SchedulingAgentRequest":
        try:
            return handler(value)
        except ValidationError as exc:
            # Log field names only: objectives and identifiers may be sensitive.
            fields = sorted({str(error["loc"][0]) if error["loc"] else "request" for error in exc.errors()})
            logger.warning("Scheduling request refused: invalid fields: %s", ", ".join(fields))
            raise

    @field_validator("preferred_date", mode="before")
    @classmethod
    def require_calendar_date(cls, value: Any) -> date:
        if isinstance(value, date) and not isinstance(value, datetime):
            return value
        if isinstance(value, str) and re.fullmatch(r"\d{4}-\d{2}-\d{2}", value):
            try:
                return date.fromisoformat(value)
            except ValueError:
                pass
        raise ValueError("preferred_date must be a valid date in YYYY-MM-DD format.")

    @field_validator("preferred_date")
    @classmethod
    def require_future_date(cls, value: date) -> date:
        if value <= datetime.now(UTC).date():
            raise ValueError("preferred_date must be in the future (UTC).")
        return value

    @field_validator("objective_text")
    @classmethod
    def validate_objective(cls, value: str) -> str:
        objective = value.strip()
        if not objective:
            raise ValueError("objective_text must not be empty or blank.")
        normalized = " ".join(objective.casefold().split())
        if any(re.search(pattern, normalized) for pattern in _INJECTION_PATTERNS):
            raise ValueError("objective_text contains a suspected instruction override or forged tool result.")
        return objective


class SchedulingAgentResponse(BaseModel):
    """Scheduling outcome; approval and bed allocation remain outside the agent."""

    workflow_id: str
    plan: list[str] = Field(default_factory=list)
    tool_results: list[ToolResult] = Field(default_factory=list)
    validation_result: ValidationResult
    status: Literal["awaiting_approval", "safe_failure"]
    admission_request_id: UUID | None = None
    failure_reason: str | None = None


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
    approval_status: ApprovalStatus | None = ApprovalStatus.PENDING
    final_outcome: str | None = None
    agent_name: str = "unspecified"
    related_entity_type: str | None = None
    related_entity_id: str | None = None

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
    workflow_id: str = ""
