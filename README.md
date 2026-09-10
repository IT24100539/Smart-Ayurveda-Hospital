# Smart Ayurveda Hospital

[![ci](https://github.com/IT24100539/Smart-Ayurveda-Hospital/actions/workflows/ci.yml/badge.svg)](https://github.com/IT24100539/Smart-Ayurveda-Hospital/actions/workflows/ci.yml)

Hospital operations platform: public **ASP.NET Core** API, internal **LangGraph** agents, **React** staff portal, and **Flutter** patient app.

## Layout

```
backend/          # ONLY public backend (Hospital.Api)
agent-service/    # localhost-only FastAPI + LangGraph
web-staff/        # React staff / admin portal
mobile-patient/   # Flutter patient app
docs/             # ADRs, ER diagram, wireframes
```

Clients never call `agent-service`. `Hospital.Api` is the only network boundary.

## Prerequisites

- .NET 8 SDK
- Python 3.12
- Node 22
- Flutter 3.41+
- Docker (Postgres 16 + Ollama for local LLM)

This machine may not have the .NET SDK or Docker yet; install those before running the API.

## Local development

```bash
docker compose up -d
```

### API

```bash
cd backend
dotnet run --project src/Hospital.Api
```

Seed users (change immediately):

- `admin@smartayurveda.local` / `ChangeMe!Admin1`
- `doctor@smartayurveda.local` / `ChangeMe!Doctor1`

Swagger: `https://localhost:7443/swagger`

### Agent service (loopback only)

```bash
cd agent-service
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python run.py
```

Listens on `127.0.0.1:8100`.

### Staff portal

```bash
cd web-staff
npm install
npm run dev
```

Copy `web-staff/.env.example` to `web-staff/.env`. The portal calls `Hospital.Api` at `VITE_API_BASE_URL` (default `http://localhost:5000/api`).

### Patient app

```bash
cd mobile-patient
flutter run
```

## Tests

```bash
dotnet test backend/Hospital.sln
pytest agent-service
npm test --prefix web-staff
flutter test --cwd mobile-patient
```

## First EF migration

After the SDK is installed:

```bash
cd backend
dotnet ef migrations add InitialCreate --project src/Hospital.Infrastructure --startup-project src/Hospital.Api
```
