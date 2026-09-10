# ADR 0009: Git branching model

- Status: Accepted
- Date: 2026-09-09

## Context

Four members work in parallel on distinct hospital capabilities. `main` must stay deployable. Integration work for agent orchestration comes later and must not block member feature branches.

## Options considered

- **Trunk-only on `main`** — collisions and undeployable `main` while four features land.
- **Per-member long-lived forks with no integration branch** — late, painful merges.
- **`main` ← `develop` ← `memberN/...`** — protected integration line, feature PRs in parallel.

## Decision

```
main            ← protected, always deployable, only updated via PR from develop
 └── develop    ← protected, integration branch, requires passing CI to merge into
      ├── member1/patient-user-management
      ├── member2/treatment-information
      ├── member3/appointment-ward-bed
      ├── member4/feedback-communication
      └── integration/agent-orchestration   (created later)
```

- `main` and `develop` are protected. No direct commits.
- Member work lands on the matching `memberN/...` branch, then a PR into `develop`.
- CI (`.github/workflows/ci.yml`) must pass before a PR can merge into `develop` or `main`.
- `main` is updated only by PR from `develop`.
- `integration/agent-orchestration` is not created until that integration task starts.

## Consequences

Each member can merge independently into `develop`. Agent orchestration is a later shared branch, not a fifth parallel feature stream from day one.

After the remote exists, enable GitHub branch protection on `main` and `develop`: require a pull request, require the `ci` workflow, and on `main` restrict the source branch to `develop`.
