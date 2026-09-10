# ADR 0007: Agent service is localhost-only

- Status: Accepted
- Date: 2026-09-09

## Context

LLM / LangGraph workflows help with intake notes, appointment suggestions, and treatment decision support. They must not become a second public API, and they must not hold canonical clinical data.

## Options considered

- **Publish `agent-service` on 0.0.0.0 / a public port** — Flutter and the staff portal could call it directly; that violates the internal-service rule and exposes the model host.
- **Call Ollama from the browser** — same leak, plus CORS and no `X-Internal-Secret`.
- **Bind to 127.0.0.1; only `Hospital.Api` calls it** — internal worker on the same host.

## Decision

Run `agent-service` as a FastAPI app bound to `127.0.0.1`. The ASP.NET API is the only caller, authenticated with `X-Internal-Secret`. Staff and patients never receive this URL.

## Consequences

Docker Compose does not publish the agent port. Production deployments keep the agent on a private network or the same host. Clinical writes still go through `Hospital.Api` and PostgreSQL.
