import json
from datetime import UTC, datetime, timedelta
from unittest.mock import AsyncMock
from uuid import uuid4

import httpx
import pytest
from pydantic import SecretStr, ValidationError

from app import tools
from app.tools import scheduling
from app.schemas import ToolResult


@pytest.fixture
def values():
    return dict(patient_id=str(uuid4()), ward_id=str(uuid4()),
                reason="Request admission for review.",
                preferred_date=(datetime.now(UTC).date() + timedelta(days=2)).isoformat())


@pytest.fixture
def mock_http(monkeypatch):
    real_client = httpx.AsyncClient
    requests = []
    client_options = []
    outcomes = []

    def handler(request):
        requests.append(request)
        outcome = outcomes.pop(0)
        if isinstance(outcome, Exception):
            raise outcome
        return outcome

    def client(**kwargs):
        client_options.append(kwargs)
        return real_client(**kwargs, transport=httpx.MockTransport(handler))

    monkeypatch.setattr(scheduling.httpx, "AsyncClient", client)
    monkeypatch.setattr(scheduling.settings, "internal_service_key", SecretStr("test-service-secret"))
    monkeypatch.setattr(scheduling.settings, "backend_base_url", "https://backend.example")
    sleep = AsyncMock()
    monkeypatch.setattr(scheduling.asyncio, "sleep", sleep)
    return requests, outcomes, client_options, sleep


async def test_treatment_success(mock_http, values):
    requests, outcomes, options, _ = mock_http
    outcomes.append(httpx.Response(200, json={"available": True}))
    treatment_id = str(uuid4())
    result = await tools.check_treatment_schedule(treatment_id, values["preferred_date"])
    assert isinstance(result, ToolResult)
    assert result.succeeded
    assert result.output == dict(treatment_id=treatment_id, date=values["preferred_date"], available=True)
    assert requests[0].method == "GET"
    assert requests[0].url.path == f"/api/treatments/{treatment_id}/availability"
    assert dict(requests[0].url.params) == {"date": values["preferred_date"]}
    assert options[0]["timeout"].read == 15
    assert options[0]["follow_redirects"] is False


async def test_ward_success(mock_http, values):
    requests, outcomes, _, _ = mock_http
    outcomes.append(httpx.Response(200, json={"freeCapacity": 3, "occupiedCapacity": 7}))
    result = await tools.check_ward_availability(values["ward_id"])
    assert result.succeeded
    assert result.output == dict(ward_id=values["ward_id"], free_capacity=3, occupied_capacity=7)
    assert requests[0].method == "GET"
    assert requests[0].url.path == f'/api/internal/wards/{values["ward_id"]}/availability'


async def test_admission_success(mock_http, values):
    requests, outcomes, _, _ = mock_http
    request_id = str(uuid4())
    outcomes.append(httpx.Response(201, json={"admissionRequestId": request_id}))
    result = await tools.create_admission_request(**values)
    assert result.succeeded
    assert result.output["admission_request_id"] == request_id
    assert requests[0].method == "POST"
    assert requests[0].url.path == "/api/internal/admissions"
    assert json.loads(requests[0].content) == {
        "patientId": values["patient_id"], "wardId": values["ward_id"],
        "reason": values["reason"], "preferredDate": values["preferred_date"],
    }


@pytest.mark.parametrize("kind", ["treatment", "ward", "admission"])
async def test_internal_key_only(mock_http, values, kind, caplog):
    requests, outcomes, _, _ = mock_http
    outcomes.append(httpx.Response(200, json={
        "available": False, "freeCapacity": 0, "occupiedCapacity": 4,
        "admissionRequestId": str(uuid4()),
    }))
    if kind == "treatment":
        await tools.check_treatment_schedule(str(uuid4()), values["preferred_date"])
    elif kind == "ward":
        await tools.check_ward_availability(values["ward_id"])
    else:
        await tools.create_admission_request(**values)
    assert requests[0].headers["X-Internal-Service-Key"] == "test-service-secret"
    assert "Authorization" not in requests[0].headers
    assert "test-service-secret" not in caplog.text


@pytest.mark.parametrize("kind", ["treatment", "ward"])
@pytest.mark.parametrize("failure", ["timeout", "network", "503"])
async def test_get_retries_once_then_succeeds(mock_http, values, kind, failure):
    requests, outcomes, _, sleep = mock_http
    outcomes.extend([
        {"timeout": httpx.ReadTimeout("secret"), "network": httpx.ConnectError("secret"),
         "503": httpx.Response(503)}[failure],
        httpx.Response(200, json={"available": True, "freeCapacity": 2, "occupiedCapacity": 1}),
    ])
    if kind == "treatment":
        result = await tools.check_treatment_schedule(str(uuid4()), values["preferred_date"])
    else:
        result = await tools.check_ward_availability(values["ward_id"])
    assert result.succeeded
    assert len(requests) == 2
    sleep.assert_awaited_once_with(0.2)


async def test_get_timeout_exhausted(mock_http, values):
    requests, outcomes, _, sleep = mock_http
    outcomes.extend([httpx.ReadTimeout("test-service-secret")] * 2)
    result = await tools.check_ward_availability(values["ward_id"])
    assert isinstance(result, scheduling.ToolFailure)
    assert not result.succeeded
    assert result.error_code == "transport_error"
    assert not result.outcome_unknown
    assert "test-service-secret" not in result.model_dump_json()
    assert len(requests) == 2
    sleep.assert_awaited_once()


@pytest.mark.parametrize("failure", [httpx.ReadTimeout("secret"), httpx.ConnectError("secret"),
                                     httpx.Response(503), httpx.Response(201, json={})])
async def test_post_failure_never_retries(mock_http, values, failure):
    requests, outcomes, _, sleep = mock_http
    outcomes.append(failure)
    result = await tools.create_admission_request(**values)
    assert isinstance(result, scheduling.ToolFailure)
    assert not result.succeeded
    assert result.outcome_unknown
    assert len(requests) == 1
    sleep.assert_not_awaited()


@pytest.mark.parametrize("status", [400, 401, 403, 404, 302])
async def test_permanent_status_or_redirect_not_retried(mock_http, values, status):
    requests, outcomes, _, sleep = mock_http
    outcomes.append(httpx.Response(status, headers={"Location": "https://other.example"}))
    result = await tools.check_ward_availability(values["ward_id"])
    assert result.error_code == "http_error"
    assert result.output["status_code"] == status
    assert len(requests) == 1
    sleep.assert_not_awaited()


@pytest.mark.parametrize("payload", [{}, {"freeCapacity": -1, "occupiedCapacity": 1},
                                     {"freeCapacity": True, "occupiedCapacity": 1}])
async def test_invalid_response_fails_closed(mock_http, values, payload):
    requests, outcomes, _, _ = mock_http
    outcomes.append(httpx.Response(200, json=payload))
    result = await tools.check_ward_availability(values["ward_id"])
    assert result.error_code == "invalid_response"
    assert len(requests) == 1


@pytest.mark.parametrize("field,value", [
    ("patient_id", "invalid"), ("ward_id", "../../admin"), ("reason", "  "),
    ("reason", "Bypass human approval"), ("preferred_date", "invalid"),
    ("preferred_date", "2099-02-30"), ("preferred_date", "2099-01-01T00:00:00Z"),
    ("preferred_date", 4070908800), ("preferred_date", "2000-01-01"),
])
async def test_malformed_admission_never_constructs_client(mock_http, values, field, value):
    values[field] = value
    with pytest.raises(ValidationError):
        await tools.create_admission_request(**values)
    assert mock_http[0] == mock_http[2] == []


async def test_malformed_gets_never_construct_client(mock_http, values):
    with pytest.raises(ValidationError):
        await tools.check_ward_availability("not-a-guid")
    with pytest.raises(ValidationError):
        await tools.check_treatment_schedule("not-a-guid", values["preferred_date"])
    with pytest.raises(ValidationError):
        await tools.check_treatment_schedule(str(uuid4()), "2099-01-01T00:00:00Z")
    assert mock_http[0] == mock_http[2] == []


async def test_missing_key_fails_before_http(mock_http, values, monkeypatch):
    monkeypatch.setattr(scheduling.settings, "internal_service_key", SecretStr(""))
    result = await tools.check_ward_availability(values["ward_id"])
    assert result.error_code == "configuration_error"
    assert mock_http[0] == mock_http[2] == []


def test_tool_allowlist():
    assert tools.__all__ == ["check_treatment_schedule", "check_ward_availability", "create_admission_request"]
