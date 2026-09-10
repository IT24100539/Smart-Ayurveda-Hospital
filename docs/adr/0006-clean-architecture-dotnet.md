# ADR 0006: Clean Architecture ASP.NET backend

- Status: Accepted
- Date: 2026-09-09

## Context

The hospital system needs a single public API for staff and patients, with room to grow (consultations, panchakarma, pharmacy, billing) without mixing transport, persistence, and domain rules.

## Options considered

- **Single ASP.NET project** — fastest start; domain, SQL, and HTTP would share one assembly and become harder to test in isolation.
- **Separate public backends** (Nest/Express plus .NET) — forbidden by the architecture: `Hospital.Api` is the only public API.
- **Four-project Clean Architecture** — Domain, Application, Infrastructure, Api.

## Decision

Use a .NET 8 solution with four projects:

- `Hospital.Domain` — entities, enums, domain exceptions
- `Hospital.Application` — use cases, DTOs, validators, repository contracts
- `Hospital.Infrastructure` — EF Core / PostgreSQL, JWT, agent HTTP client
- `Hospital.Api` — controllers, middleware, composition root

`Hospital.Api` is the only process exposed to browsers and mobile apps.

## Consequences

New features follow Domain → Application → Infrastructure → Api. The Python agent service is not a public backend and must not duplicate hospital records as a source of truth.
