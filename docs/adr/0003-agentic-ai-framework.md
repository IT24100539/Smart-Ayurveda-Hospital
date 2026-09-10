# ADR 0003: LangGraph and Ollama for agents

- Status: Accepted
- Date: 2026-09-10

## Context

The internal agent service must plan, delegate to specialist agents,
validate, and wait for a vaidya’s approval. It runs only behind
`Hospital.Api` (localhost, never a public API). The lab stack is Python +
local models. Paid cloud keys and rate limits would block demos.

## Options considered

- **Custom orchestration script** — a hand-rolled loop of if/else and HTTP
  calls. Fast to start, no graph, no durable state machine, hard to extend
  to four agents plus approval.
- **Hosted paid API** (OpenAI, Anthropic, cloud agents) — strong models,
  requires keys, billing, and network at demo time.
- **LangGraph + Ollama** — graph as a state machine; ChatOllama against a
  local `OLLAMA_BASE_URL` / `OLLAMA_MODEL`.

## Decision

Use LangGraph for orchestration and LangChain’s Ollama chat wrapper. The
graph maps directly onto plan → delegate → validate → approve. Ollama keeps
the project free to run and demo with no API keys or rate limits, which
matches the lab stack.

Default model: `llama3.1:8b` at `http://localhost:11434`. No paid keys in
repo or compose files.

## Consequences

Developers need Ollama running locally (see ADR 0008). Graphs stay in
`agent-service/app/graph/`; one file per agent under `app/agents/`. Do not
call a cloud LLM from this service without a new ADR.
