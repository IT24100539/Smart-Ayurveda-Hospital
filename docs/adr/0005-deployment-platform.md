# ADR 0005: Deployment platform

- Status: Proposed (TBD)
- Date: 2026-09-10

## Context

The stack is an ASP.NET Core API, PostgreSQL, a localhost-only Python agent
service + Ollama, a Vite React staff portal, and a Flutter patient app.
Hosting choices affect secrets, CORS, and whether the agent can stay off
the public internet. This is recorded now so the integration/deployment
phase has a short list, not a blank page.

## Options considered

- **API:** Render / Railway / Fly.io — each can run a Dockerized
  `Hospital.Api`. Fly.io is closer to a private network for the agent;
  Render and Railway are simpler student deploys.
- **Postgres:** Neon / Supabase — hosted Postgres with a connection string
  the API already understands. Supabase adds extras we do not need yet.
- **React staff portal:** Vercel / Netlify — static Vite output, env-based
  API URL.
- **Agent + Ollama:** not listed as a public host. They stay on a private
  host or the same machine as the API (ADR 0007).

Flutter stores are out of scope for web hosting.

## Decision

TBD. Finalize in the integration/deployment phase once we know whether the
agent can colocate with the API and which free tiers still fit a demo.

## Consequences

No production URLs or vendor lock-in until then. Local `docker-compose`
(Postgres + Ollama) remains the supported dev path. A later ADR will
replace this one when the platforms are chosen.
