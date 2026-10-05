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

## Run the project

PostgreSQL must be listening on port 5432, and Ollama must be serving `llama3.1` on port 11434. From the repo root:

```powershell
.\scripts\dev-start.ps1
```

The script stops listeners on the dev ports, checks Postgres, Ollama, and the HTTPS dev certificate, syncs `InternalServiceKey` into `agent-service\.env` as `AGENT_INTERNAL_SERVICE_KEY` without printing the value, and opens a window each for the API, the agent (port 8100), and the staff portal.

```powershell
.\scripts\dev-stop.ps1
```

That stops only the processes listening on 7443, 5080, 8100, 5173, and 5174, including child processes such as reload workers.

`.\scripts\dev-start.ps1 -Only backend` starts one service. `-Only backend,agent,web,mobile` also starts the Flutter web app. `-SkipStop` leaves current listeners in place.

| Service | URL |
| --- | --- |
| API | https://localhost:7443 and http://localhost:5080 |
| API health | https://localhost:7443/api/health |
| Staff portal | http://localhost:5173 |
| Agent docs | http://127.0.0.1:8100/docs |
| Patient web (`-Only mobile`) | http://localhost:5174 |

## Installation and startup

Prerequisites: .NET 8 SDK, Python 3.12, Node 22, Flutter 3.41+, Docker (Postgres 16 and Ollama).

`appsettings.json` has no passwords or signing keys. Copy `.env.example` to `.env` for Docker and the agent. Put the API secrets in user-secrets (Development) or environment variables (any host). Do not commit the values.

```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=ayurveda_hospital;Username=postgres;Password=<your-db-password>" --project src/Hospital.Api
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-characters>" --project src/Hospital.Api
dotnet user-secrets set "AgentService:SharedSecret" "<same-value-as-AGENT_SHARED_SECRET>" --project src/Hospital.Api
dotnet user-secrets set "INTERNAL_SERVICE_KEY" "<same-value-as-the-agent>" --project src/Hospital.Api
```

The same keys work as environment variables: `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey` (or `Jwt__Secret`), `AgentService__SharedSecret` (or `AGENT_SHARED_SECRET`), and `INTERNAL_SERVICE_KEY`.

| Variable | Used by |
| --- | --- |
| `POSTGRES_PASSWORD` | `docker compose` |
| `ConnectionStrings__DefaultConnection` | API and `dotnet ef` |
| `Jwt__SigningKey` or `Jwt__Secret` | API signing key, at least 32 characters |
| `Jwt__Issuer`, `Jwt__Audience` | API token validation |
| `INTERNAL_SERVICE_KEY` | API and agent, header `X-Internal-Service-Key` |
| `AGENT_HOSPITAL_API_BASE_URL`, `AGENT_BACKEND_BASE_URL` | Agent → API origin, default `http://127.0.0.1:5080`, no `/api` suffix |
| `AGENT_PORT` | Agent listen port, default `8001` |
| `AGENT_SHARED_SECRET` | Same value as `AgentService:SharedSecret` (`X-Internal-Secret`, API → agent) |
| `AGENT_OLLAMA_BASE_URL` | Default `http://127.0.0.1:11434` |
| `AllowedOrigins` | Production CORS: staff web origin, then Flutter web origin. `https` only |
| `Kestrel__Certificates__Default__Path` and `Kestrel__Certificates__Default__Password` | Production certificate when this process terminates TLS. Omit both when `PORT` is set (Render terminates TLS) |
| `VITE_API_BASE_URL` | Staff portal, default `http://localhost:5080/api` |

Development serves HTTPS on `https://localhost:7443` with the ASP.NET development certificate (`dotnet dev-certs https --trust`). Production redirects to HTTPS and sends HSTS unless `PORT` is set, in which case the host terminates TLS. Production CORS allows only `AllowedOrigins`, or `Cors:StaffOrigins` plus `Cors:FlutterWebOrigins`. Startup fails if a secret is missing or still a development placeholder.

### 1. PostgreSQL and Ollama

```bash
docker compose up -d
docker exec sah-ollama ollama pull llama3.1
```

Postgres listens on `localhost:5432`, database `ayurveda_hospital`. Ollama listens on `http://127.0.0.1:11434`.

### Shared internal key

The API and the agent both read `INTERNAL_SERVICE_KEY` and send it as `X-Internal-Service-Key`. Generate a random value and store that same value in the gitignored repo-root `.env` and in backend user-secrets. Do not commit it.

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
# Put INTERNAL_SERVICE_KEY=<that value> in the repo-root .env
cd backend
dotnet user-secrets set "INTERNAL_SERVICE_KEY" "<that value>" --project src/Hospital.Api
```

An empty key is rejected. In Development the API serves `/api/internal/*` on plain HTTP (`http://127.0.0.1:5080`) and does not redirect those calls to HTTPS.

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

Listens on `127.0.0.1:8001` (`AGENT_PORT` overrides the port). Check `http://127.0.0.1:8001/health`. The API calls that origin (`AgentService:BaseUrl`, default `http://127.0.0.1:8001`). Start Ollama before a coordinate or scheduling-bed call. Full sequence: [docs/deployment-agent-service.md](docs/deployment-agent-service.md).

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
k6 run perf/k6/scheduling-agent.js -e AGENT_URL=http://127.0.0.1:8001
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
| Agent + Ollama | [docs/deployment-agent-service.md](docs/deployment-agent-service.md) | local `http://127.0.0.1:8001` for the demo |

Test accounts (seeded when the user table is empty; change them on any shared host):

- Admin: `admin@smartayurveda.local` / `ChangeMe!Admin1`
- Doctor: `doctor@smartayurveda.local` / `ChangeMe!Doctor1`
- Patient: `meera.nair@example.local` / `ChangeMe!Patient1`

## Group AI usage declaration

Each member's use of AI tools is logged in their own `docs/individual/memberN-contribution.md`. Member 4's log is [docs/individual/member4-contribution.md](docs/individual/member4-contribution.md). Members 1, 2, and 3 keep the same record in `docs/individual/member1-contribution.md`, `member2-contribution.md`, and `member3-contribution.md`. Every member can explain, test, and modify the code submitted under their name.
