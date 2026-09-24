"""The only Scheduling & Bed Agent tools (async, no graph registration).

Proposed backend contracts, pending backend integration:
* GET /api/treatments/{id}/availability?date=YYYY-MM-DD
  -> {"available": bool}
* GET /api/internal/wards/{id}/availability
  -> {"freeCapacity": int >= 0, "occupiedCapacity": int >= 0}
* POST /api/internal/admissions with patientId, wardId, reason, preferredDate
  -> {"admissionRequestId": GUID}

Responses may include additional fields; only validated fields enter ToolResult.
Snake-case equivalents are accepted too. Output uses snake_case and includes
the validated query context. Availability is a snapshot, never an allocation.
Invalid inputs raise Pydantic ValidationError before constructing an HTTP client.
Operational failures return ToolFailure, a shared ToolResult subtype.

POST is NEVER retried: without backend idempotency even a timeout or 5xx may
follow a committed write. outcome_unknown tells a future graph to fail safely
and reconcile with the backend, rather than automatically submitting again.
"""

import asyncio
from datetime import date as CalendarDate
from typing import Annotated, Literal
from uuid import UUID

import httpx
from pydantic import (
    BaseModel, BeforeValidator, AfterValidator, ConfigDict, Field,
    StrictBool, ValidationError, field_validator,
)

from app.schemas import SchedulingAgentRequest, ToolResult
from app.settings import settings

__all__ = [
    "check_treatment_schedule", "check_ward_availability", "create_admission_request",
]

_FutureDate = Annotated[
    CalendarDate,
    BeforeValidator(SchedulingAgentRequest.require_calendar_date),
    AfterValidator(SchedulingAgentRequest.require_future_date),
]
_Capacity = Annotated[int, Field(strict=True, ge=0)]
_TRANSIENT_STATUS = {408, 429, 500, 502, 503, 504}


class _Input(BaseModel):
    model_config = ConfigDict(extra="forbid")


class TreatmentScheduleInput(_Input):
    treatment_id: UUID
    date: _FutureDate


class WardAvailabilityInput(_Input):
    ward_id: UUID


class AdmissionRequestInput(_Input):
    patient_id: UUID
    ward_id: UUID
    reason: str
    preferred_date: _FutureDate

    @field_validator("reason")
    @classmethod
    def validate_reason(cls, value: str) -> str:
        return SchedulingAgentRequest.validate_objective(value)


class _Response(BaseModel):
    model_config = ConfigDict(populate_by_name=True)


class _TreatmentAvailability(_Response):
    available: StrictBool


class _WardAvailability(_Response):
    free_capacity: _Capacity = Field(alias="freeCapacity")
    occupied_capacity: _Capacity = Field(alias="occupiedCapacity")


class _AdmissionCreated(_Response):
    admission_request_id: UUID = Field(alias="admissionRequestId")


class ToolFailure(ToolResult):
    succeeded: Literal[False] = False
    error_code: Literal["configuration_error", "transport_error", "http_error", "invalid_response"]
    outcome_unknown: bool = False


async def _request(
    tool: str, method: str, path: str, response_model: type[_Response],
    context: dict, *, params: dict | None = None, body: dict | None = None,
) -> ToolResult:
    key = settings.internal_service_key.get_secret_value()
    if not key.strip():
        return ToolFailure(tool=tool, error_code="configuration_error",
                           error="Backend service authentication is not configured.")
    # No staff JWT, redirects, proxy environment, or implicit transport retries.
    async with httpx.AsyncClient(
        base_url=str(settings.backend_base_url),
        headers={"X-Internal-Service-Key": key},
        timeout=httpx.Timeout(15.0), follow_redirects=False, trust_env=False,
    ) as client:
        attempts = 2 if method == "GET" else 1
        for attempt in range(attempts):
            try:
                response = await client.request(method, path, params=params, json=body)
            except httpx.RequestError:
                if attempt + 1 < attempts:
                    await asyncio.sleep(0.2 * (2 ** attempt))
                    continue
                # Never expose exception text, request headers, or backend bodies.
                return ToolFailure(tool=tool, error_code="transport_error",
                                   error="Backend request failed; stop safely.",
                                   outcome_unknown=method == "POST")
            if response.status_code in _TRANSIENT_STATUS and attempt + 1 < attempts:
                await asyncio.sleep(0.2 * (2 ** attempt))
                continue
            if not response.is_success:
                return ToolFailure(tool=tool, error_code="http_error",
                                   error="Backend returned an unsuccessful HTTP status.",
                                   output={"status_code": response.status_code},
                                   outcome_unknown=method == "POST")
            try:
                output = response_model.model_validate(response.json()).model_dump(mode="json")
            except (ValueError, ValidationError):
                return ToolFailure(tool=tool, error_code="invalid_response",
                                   error="Backend response did not match the tool contract.",
                                   outcome_unknown=method == "POST")
            return ToolResult(tool=tool, succeeded=True, output={**context, **output})


async def check_treatment_schedule(treatment_id: UUID | str, date: CalendarDate | str) -> ToolResult:
    query = TreatmentScheduleInput(treatment_id=treatment_id, date=date)
    context = query.model_dump(mode="json")
    return await _request(
        "check_treatment_schedule", "GET",
        f"/api/treatments/{query.treatment_id}/availability", _TreatmentAvailability,
        context, params={"date": context["date"]},
    )


async def check_ward_availability(ward_id: UUID | str) -> ToolResult:
    query = WardAvailabilityInput(ward_id=ward_id)
    return await _request(
        "check_ward_availability", "GET",
        f"/api/internal/wards/{query.ward_id}/availability", _WardAvailability,
        query.model_dump(mode="json"),
    )


async def create_admission_request(
    patient_id: UUID | str, ward_id: UUID | str, reason: str,
    preferred_date: CalendarDate | str,
) -> ToolResult:
    query = AdmissionRequestInput(
        patient_id=patient_id, ward_id=ward_id, reason=reason, preferred_date=preferred_date,
    )
    context = query.model_dump(mode="json")
    return await _request(
        "create_admission_request", "POST", "/api/internal/admissions", _AdmissionCreated,
        {k: v for k, v in context.items() if k != "reason"},
        body={"patientId": context["patient_id"], "wardId": context["ward_id"],
              "reason": query.reason, "preferredDate": context["preferred_date"]},
    )
