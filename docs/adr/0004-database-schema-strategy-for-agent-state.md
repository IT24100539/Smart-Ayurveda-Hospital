# ADR 0004: Agent workflow state lives in PostgreSQL

- Status: Accepted
- Date: 2026-09-10

## Context

Agent runs produce a `WorkflowState` (plan, tool results, validation,
approval, outcome). Staff in the React portal must review and query that
history. The Python process can restart, scale, or crash; anything stored
only in memory is lost and invisible to the API.

The assignment requires agent state in Postgres, not in the agent process.
`Hospital.Api` is the only component that talks to the database.

## Options considered

- **In-process dict / LangGraph checkpointer in Python** — simple, dies with
  the worker, not auditable from React.
- **Python talks to Postgres directly** — second data path, duplicates EF
  Core, fights “API is the only public backend.”
- **Postgres via ASP.NET Core** — API owns a `WorkflowExecutions` table;
  Python keeps only in-flight state and persists through the API.

## Decision

Persist workflow state in PostgreSQL through the ASP.NET Core backend, in a
`WorkflowExecutions` table. `agent-service` holds only transient in-flight
state (`InMemoryWorkflowStateStore` behind `WorkflowStateStore`). Member 3/4
swap that store for an HTTP implementation that calls `Hospital.Api`; the
Python process is never the system of record.

## Consequences

Approvals and history survive restarts and are queryable by staff. Schema
and migrations stay in `Hospital.Infrastructure`. Do not add a Postgres
driver to `agent-service`.
