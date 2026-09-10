# ADR 0002: Riverpod for the patient app

- Status: Accepted
- Date: 2026-09-10

## Context

The Flutter patient app (`mobile-patient`) must share auth, locale, and API
access across screens and `GoRouter` redirects. State has to be testable
without pumping a full widget tree, and mistakes (missing providers, wrong
types) should fail at compile time where possible.

## Options considered

- **Provider** — familiar and light, but lookups are stringly/runtime-typed
  and easy to mis-wire as the tree grows.
- **Bloc** — explicit events and states, strong for large event-driven apps;
  more files and boilerplate than this app’s screen count justifies.
- **Riverpod** — compile-safe providers, overrides in tests, no `BuildContext`
  required to read state.

## Decision

Use Riverpod (`flutter_riverpod`). Compile-safe providers and straightforward
test overrides fit a multi-screen patient app; it is less ceremony than Bloc
for a project this size.

Auth and routing already follow this: `ProviderScope` in `main.dart`,
`authControllerProvider` driving `GoRouter` refresh.

## Consequences

Widget tests wrap with `ProviderScope` and override providers instead of
mocking `BuildContext`. Teammates learn Riverpod’s `ref`/`ConsumerWidget`
model rather than Bloc’s event/state classes. Do not introduce Provider or
Bloc alongside Riverpod in `mobile-patient`.
