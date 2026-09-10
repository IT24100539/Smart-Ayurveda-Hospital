# ADR 0001: Zustand for staff-portal state

- Status: Accepted
- Date: 2026-09-10

## Context

The React staff portal (`web-staff`) needs auth and session state shared
across routes: a JWT after login, the current user, and a logout path that
clears both. That state must survive a refresh and stay readable from the
API client, protected routes, and the shell. Prompt 0.4 already chose
Zustand; this ADR records why.

## Options considered

- **Context API** — built in, but auth updates would re-render large trees
  and we would still invent persist/subscribe helpers.
- **Redux Toolkit** — excellent DevTools and middleware, more store/slice
  ceremony than a hospital SPA of this size needs.
- **Zustand** — a small store with optional `persist`, no providers wrapping
  the tree.

## Decision

Use Zustand. The portal is a handful of routes and one auth session, not a
complex client cache. Zustand’s boilerplate is minimal and we do not need
Redux’s middleware ecosystem (sagas, RTK Query, action replay).

Session persistence lives in `web-staff/src/store/authStore.ts` (`persist` +
`localStorage`). See also ADR 0010 for the JWT-in-`localStorage` trade-off.

## Consequences

Teammates can read and extend the store without Redux vocabulary. DevTools
and time-travel debugging are weaker than Redux. Do not add a second state
library in `web-staff`; new shared client state goes in Zustand stores.
