from fastapi.testclient import TestClient

from app.main import app


def test_health():
    client = TestClient(app)
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "ok"


def test_invoke_requires_secret():
    client = TestClient(app)
    response = client.post("/v1/invoke", json={"agent": "intake", "prompt": "hello"})
    assert response.status_code == 401


def test_invoke_does_not_serve_the_old_feedback_stub():
    client = TestClient(app)
    response = client.post(
        "/v1/invoke",
        json={
            "agent": "feedback",
            "prompt": "Draft a reply",
            "context": {"rating": "2", "comment": "The nadi pariksha slot ran late", "anonymous": "true"},
        },
        headers={"X-Internal-Secret": "dev-internal-agent-secret"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["agent"] != "feedback"
    assert "draft reply for staff review" not in body["reply"].lower()


def test_invoke_routes_appointment():
    client = TestClient(app)
    response = client.post(
        "/v1/invoke",
        json={"agent": "coordinator", "prompt": "Schedule an appointment tomorrow"},
        headers={"X-Internal-Secret": "dev-internal-agent-secret"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["agent"] == "appointment"
    assert "Appointment" in body["reply"]
