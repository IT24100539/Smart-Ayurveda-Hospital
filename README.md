# Smart Ayurveda Hospital

[![ci](https://github.com/IT24100539/Smart-Ayurveda-Hospital/actions/workflows/ci.yml/badge.svg)](https://github.com/IT24100539/Smart-Ayurveda-Hospital/actions/workflows/ci.yml)

## Project overview

Smart Ayurveda Hospital is an operations platform for an Ayurveda hospital: patients, prakriti and vikriti, treatments such as panchakarma, appointments, ward beds, and post-visit feedback. One public API (`Hospital.Api`) serves the React staff portal and the Flutter patient app. An internal LangGraph service plans work and waits for a vaidya to approve it. Browsers and phones never call the agent service.

## Business problem

Front desk, vaidyas, and patients currently split scheduling, bed allocation, and follow-up across separate conversations. A double-booked therapy slot or an unreviewed AI reply is a clinical and operational failure. This system keeps one record of the patient, the treatment schedule, and the ward, and it keeps model output as a proposal until staff accept it.

## User roles

| Role | What they do |
| --- | --- |
| Patient | Register, book a treatment, submit feedback, read replies and notifications. |
| FrontDeskStaff | Look up patients, manage appointments and ward occupancy. |
| Doctor | Review the schedule and approve or reject agent plans (AI approvals). |
| Therapist | Listed on treatment schedules. |
| Admin | Staff administration, treatments, wards, feedback moderation, and AI approvals. |

## Features

- Patient registration and staff search, with UHID, prakriti, and vikriti.
- Treatment catalogue and weekly schedules (capacity per slot).
- Appointment booking that rejects a second patient when the last seat is taken.
- Ward occupancy and admission requests. Approving an admission assigns a free bed.
- Feedback, complaints, reactions, and notifications. Agent reply drafts stay unpublished until staff post them.
- Coordinator workflow: classify an objective, delegate to one specialist, persist the run, and let staff approve, reject, or request a revision.

## Technology justification

| Choice | Why |
| --- | --- |
| ASP.NET Core 8, Clean Architecture | One public API. Domain rules stay out of HTTP and SQL. [ADR 0006](docs/adr/0006-clean-architecture-dotnet.md). |
| PostgreSQL 16 | Relational hospital data and durable agent workflow JSON. [ADR 0008](docs/adr/0008-postgres-and-ollama.md), [ADR 0004](docs/adr/0004-database-schema-strategy-for-agent-state.md). |
| React, Vite, Zustand | Staff portal. Session state is a small store, not a second framework. [ADR 0001](docs/adr/0001-react-state-management.md), [ADR 0010](docs/adr/0010-staff-jwt-localstorage.md). |
| Flutter, Riverpod | Patient app on Android, with the API base URL fixed at build time. [ADR 0002](docs/adr/0002-flutter-state-management.md). |
| LangGraph + Ollama (`llama3.1`) | Local model, no paid API key, graph of plan then delegate then approve. [ADR 0003](docs/adr/0003-agentic-ai-framework.md). |
| Render, Neon, Vercel | Free-tier hosts for the API, Postgres, and staff portal. The agent stays off those hosts. [ADR 0005](docs/adr/0005-deployment-platform.md), [ADR 0007](docs/adr/0007-agent-service-localhost-only.md). |

## System architecture

`web-staff` and `mobile-patient` call `Hospital.Api` only. The API calls `agent-service` on loopback with `X-Internal-Secret`. The agent calls the API with `X-Internal-Service-Key`. Workflow snapshots are rows in PostgreSQL, not process memory.

Architecture decisions:

- [ADR 0001](docs/adr/0001-react-state-management.md) Zustand
- [ADR 0002](docs/adr/0002-flutter-state-management.md) Riverpod
- [ADR 0003](docs/adr/0003-agentic-ai-framework.md) LangGraph and Ollama
- [ADR 0004](docs/adr/0004-database-schema-strategy-for-agent-state.md) workflow state in PostgreSQL
- [ADR 0005](docs/adr/0005-deployment-platform.md) deployment platform
- [ADR 0006](docs/adr/0006-clean-architecture-dotnet.md) Clean Architecture
- [ADR 0007](docs/adr/0007-agent-service-localhost-only.md) agent is localhost-only
- [ADR 0008](docs/adr/0008-postgres-and-ollama.md) Postgres and Ollama
- [ADR 0009](docs/adr/0009-git-branching.md) branching
- [ADR 0010](docs/adr/0010-staff-jwt-localstorage.md) staff JWT storage

## Agentic AI architecture

`POST /internal/agents/coordinate` (`agent-service/app/graph/coordinator.py`) is the single start. Intake asks Ollama to pick one specialist. Delegate calls that agent's existing entrypoint. Aggregate returns `workflow_id`, who ran, a summary, approval status, and final outcome. Staff read the run and decide from the portal (`PATCH /api/agent-workflows/{id}/approve`).

| Specialist | Code |
| --- | --- |
| `patient_info` | `agent-service/app/agents/intake_agent.py` (no separate patient-info module) |
| `treatment_info` | `agent-service/app/agents/treatment_info_agent.py` |
| `scheduling_bed` | `agent-service/app/agents/scheduling_bed_agent.py` |
| `feedback_support` | `agent-service/app/agents/feedback_support_agent.py` |

## Database design

The entity overview is the Mermaid diagram in [docs/er-diagram/README.md](docs/er-diagram/README.md). Tables that the API actually migrates (patients, treatments, schedules, appointments, wards, beds, admissions, feedback, complaints, workflow executions) are the EF Core migrations under `backend/src/Hospital.Infrastructure/Persistence/Migrations/`.

## Repository structure

```
backend/            Hospital.Api, Application, Domain, Infrastructure
agent-service/      FastAPI + LangGraph, bind 127.0.0.1
web-staff/          React staff portal
mobile-patient/     Flutter patient app
perf/k6/            k6 performance scripts
docs/adr/           architecture decisions
docs/er-diagram/    entity diagram
docs/deployment-*.md
docs/individual/    per-member contribution and AI logs
```

## Installation and startup

Prerequisites: .NET 8 SDK, Python 3.12, Node 22, Flutter 3.41+, Docker (Postgres 16 and Ollama).

Copy `.env.example` to `.env`. Local placeholders:

| Variable | Used by |
| --- | --- |
| `POSTGRES_PASSWORD` | `docker compose` |
| `ConnectionStrings__DefaultConnection` | API and `dotnet ef` (Host, Port, Database, Username, Password) |
| `Jwt__Secret` or `Jwt__SigningKey` | API signing key, at least 32 characters |
| `Jwt__Issuer`, `Jwt__Audience` | API token validation |
| `InternalServiceKey` or `InternalService__ApiKey` | API internal header |
| `AGENT_HOSPITAL_API_BASE_URL`, `AGENT_BACKEND_BASE_URL` | Agent → API origin, no `/api` suffix |
| `AGENT_INTERNAL_SERVICE_KEY` | Same value as the API internal key |
| `AGENT_SHARED_SECRET` | Same value as `AgentService:SharedSecret` |
| `AGENT_OLLAMA_BASE_URL` | Default `http://127.0.0.1:11434` |
| `VITE_API_BASE_URL` | Staff portal, default `http://localhost:5080/api` |

### 1. PostgreSQL and Ollama

```bash
docker compose up -d
docker exec sah-ollama ollama pull llama3.1
```

Postgres listens on `localhost:5432`, database `ayurveda_hospital`. Ollama listens on `http://127.0.0.1:11434`.

Apply the schema from `backend/` (the API also migrates on startup):

```bash
dotnet ef database update --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```

### 2. Backend

```bash
cd backend
dotnet run --project src/Hospital.Api
```

HTTP `http://localhost:5080`, HTTPS `https://localhost:7443`. Swagger: `https://localhost:7443/swagger` (also `/swagger` on the HTTP port). Health: `http://localhost:5080/api/health`.

### 3. React staff portal

```bash
cd web-staff
npm install
npm run dev
```

Copy `web-staff/.env.example` to `web-staff/.env` if you need to override `VITE_API_BASE_URL`. Dev server: `http://localhost:5173`.

### 4. Flutter patient app

```bash
cd mobile-patient
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/api
```

`10.0.2.2` is the Android emulator's route to this machine. Use `http://localhost:5080/api` for Flutter web.

### 5. Agent service

```bash
cd agent-service
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python run.py
```

Listens on `127.0.0.1:8100`. Check `http://127.0.0.1:8100/health`. Start Ollama before a coordinate or scheduling-bed call. Full sequence: [docs/deployment-agent-service.md](docs/deployment-agent-service.md).

## API documentation

Local Swagger UI: `https://localhost:7443/swagger`.

After deployment, Swagger is `https://<render-host>/swagger`. That host is not assigned yet.

## Tests

```bash
dotnet test backend/Hospital.sln
cd agent-service && pytest tests/ -v
npm test --prefix web-staff
cd mobile-patient && flutter test
```

Performance scripts (API and agent already running): [docs/performance-report.md](docs/performance-report.md).

```bash
k6 run perf/k6/treatments.js -e BASE_URL=http://127.0.0.1:5000
k6 run perf/k6/double-booking.js -e BASE_URL=http://127.0.0.1:5000
k6 run perf/k6/scheduling-agent.js -e AGENT_URL=http://127.0.0.1:8100
```

## Deployment

Free tier only. The live demo runs the whole stack on one machine, including Ollama. Cloud hosting is the API, Postgres, and staff portal, not the agent.

| Piece | Guide | Live URL |
| --- | --- | --- |
| API (Render) | [docs/deployment-backend.md](docs/deployment-backend.md) | `https://<render-host>` (not deployed yet) |
| Swagger | same | `https://<render-host>/swagger` |
| Health | same | `https://<render-host>/api/health` |
| Postgres (Neon) | [docs/deployment-backend.md](docs/deployment-backend.md) | connection string in the API env, not a browser URL |
| Staff portal (Vercel) | [docs/deployment-web-staff.md](docs/deployment-web-staff.md) | `https://<project>.vercel.app` (not deployed yet) |
| Patient APK | [docs/deployment-mobile-patient.md](docs/deployment-mobile-patient.md) | `mobile-patient/build/app/outputs/flutter-apk/app-release.apk` |
| Agent + Ollama | [docs/deployment-agent-service.md](docs/deployment-agent-service.md) | local `http://127.0.0.1:8100` for the demo |

Test accounts (seeded when the user table is empty; change them on any shared host):

- Admin: `admin@smartayurveda.local` / `ChangeMe!Admin1`
- Doctor: `doctor@smartayurveda.local` / `ChangeMe!Doctor1`
- Patient: `meera.nair@example.local` / `ChangeMe!Patient1`

## Group AI usage declaration

Each member's use of AI tools is logged in their own `docs/individual/memberN-contribution.md`. Member 4's log is [docs/individual/member4-contribution.md](docs/individual/member4-contribution.md). Members 1, 2, and 3 keep the same record in `docs/individual/member1-contribution.md`, `member2-contribution.md`, and `member3-contribution.md`. Every member can explain, test, and modify the code submitted under their name.
