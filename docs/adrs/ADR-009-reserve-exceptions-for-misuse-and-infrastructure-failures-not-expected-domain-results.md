# ADR-009: Reserve Exceptions For Misuse And Infrastructure Failures, Not Expected Domain Results

- Status: Accepted
- Date: 2026-04-02

## Context

ADR-002 established `ErrorOr` as the preferred shape for expected failures at application boundaries. After introducing generic repositories and factory-based domain creation, the remaining question is where exceptions still belong.

There are still two broad categories of failure:

- expected failures caused by normal request input or domain state
- exceptional failures caused by programmer misuse or broken infrastructure assumptions

## Decision

We will return `ErrorOr` for expected failures such as:

- invalid domain creation input
- duplicate rows during normal command handling
- missing tables or rows
- invalid query shape

We will keep exceptions only for situations that indicate misuse of an internal abstraction or a broken infrastructure invariant, such as calling an in-memory repository update for an entity that was never loaded or stored through the intended application flow.

## Rationale

- Expected emulator behavior should stay explicit and mappable to protocol responses.
- Internal misuse should still fail loudly so implementation bugs are visible during development.
- This keeps ADR-002 practical without pretending every low-level invariant should be encoded as a user-facing result.

## Consequences

- Handlers should pre-check normal conflict and existence cases and return `ErrorOr`.
- Repository exceptions should be rare and signal a bug or misuse, not a normal business outcome.
- When a new throw site appears, we should ask whether it represents a real exceptional condition or an expected emulator result.
