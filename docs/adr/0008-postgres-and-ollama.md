# ADR 0008: PostgreSQL and Ollama for local development

- Status: Accepted
- Date: 2026-09-09

## Context

Staff need a relational store for patients, appointments, and billing. Agents need a local model host so development does not depend on a cloud LLM key.

## Options considered

- **SQLite / in-memory only** — fine for unit tests, not for a multi-service hospital schema or staff queries.
- **Cloud LLM instead of Ollama** — paid keys and rate limits; rejected in ADR 0003.
- **Compose: PostgreSQL 16 + Ollama** — matches the API connection string and `OLLAMA_BASE_URL`.

## Decision

`docker-compose.yml` runs PostgreSQL 16 and Ollama. The API connection string points at `localhost:5432`. The agent service points at `http://127.0.0.1:11434`.

## Consequences

Developers must start Compose (or equivalent) before the API. CI can use in-memory EF for integration tests and does not require Ollama.
