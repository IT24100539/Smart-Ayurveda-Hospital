# ADR 0010: JWT in localStorage for the staff portal

- Status: Accepted
- Date: 2026-09-09

## Context

The React staff portal (`web-staff`) is a Vite SPA. It calls `Hospital.Api` with a bearer JWT after `POST /api/auth/login`. The browser must retain the token across refreshes so protected routes and `Authorization` headers keep working.

httpOnly cookies are the stronger default for production: JavaScript cannot read them, which reduces the impact of XSS. That approach needs the API to set a `Secure` cookie, CORS credentials, and CSRF protection. This is a student hospital project with a local API and no production cookie domain yet.

## Options considered

- **httpOnly cookie from `Hospital.Api`** — better XSS posture; needs CSRF, CORS credentials, and a real cookie domain.
- **Memory-only token** — lost on refresh; protected routes would bounce to login every time.
- **`localStorage` via the Zustand `authStore`** — survives refresh; token is JS-readable.

## Decision

Persist the JWT (and user summary) in `localStorage` via the Zustand `authStore`. The `api` module reads the token from that store and attaches `Authorization: Bearer …`.

Treat this as a pragmatic trade-off, not a production pattern. The store documents the same limitation in code. State library choice is ADR 0001.

## Consequences

- XSS in the staff portal can steal the token until it expires.
- Production should move to an httpOnly, `Secure`, `SameSite` cookie issued by `Hospital.Api` (or a BFF), short-lived access tokens, and rotation. Do not keep long-lived JWTs in `localStorage`.
