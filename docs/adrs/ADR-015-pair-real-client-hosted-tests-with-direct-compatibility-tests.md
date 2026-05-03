# ADR-015: Pair Real-Client Hosted Tests With Direct Compatibility Tests

- Status: Accepted
- Date: 2026-05-04

## Context

The emulator has two different verification needs:

- prove that a real external contract works end to end
- debug and protect narrow translation, orchestration, and fault-shaping rules quickly

Hosted Aspire tests and the `net48` `CrmServiceClient` harness are strong external-contract proofs, but they are too coarse for every compatibility edge:

- they are slower to run
- they can be harder to debug when a narrow fault-shaping rule regresses
- some environments may not be able to execute every hosted harness scenario directly

Direct integration tests provide faster and more focused feedback, but they are not enough by themselves to prove hosted wire compatibility.

## Decision

We will pair real-client hosted tests with direct compatibility tests.

The expected pattern is:

- use direct integration tests for bounded request translation, orchestration, rollback, and fault-shaping behavior
- use hosted Aspire tests and the real-client harness when the change is meant to prove an external contract or bootstrap path
- keep both layers aligned to the same bounded compatibility slice instead of allowing them to drift into different stories

## Rationale

- Hosted tests prove that the emulator behaves like a real service, not just an in-proc abstraction.
- Direct tests make compatibility work faster to iterate on and easier to diagnose.
- The combination reduces the risk of either false confidence from unit-like tests or unproductive debugging from only coarse end-to-end failures.

## Consequences

- New Xrm compatibility slices should usually land with direct integration coverage first.
- Hosted verification should be added when a slice changes real-client behavior materially or proves a new external contract.
- Environment-specific hosted limitations do not invalidate the direct-compatibility layer; they should be reported clearly rather than forcing all verification into one bucket.
- Compatibility docs should describe the supported slice once, while the test suite proves it at multiple levels.
