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
