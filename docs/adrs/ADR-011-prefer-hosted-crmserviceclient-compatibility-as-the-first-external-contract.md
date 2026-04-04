# ADR-011: Prefer Hosted CrmServiceClient Compatibility As The First External Contract

- Status: Accepted
- Date: 2026-04-03

## Context

The emulator is a local-development product. The strongest early proof is not an emulator-owned client library, but an existing application using its current Dataverse/Xrm bootstrap pattern and connecting to a locally hosted emulator instead.

For the current contributor and user audience, that means:

- Aspire-hosted local execution
- connection-string driven bootstrap
- real `CrmServiceClient` compatibility
- a shared in-memory core underneath

## Decision

We will treat hosted `CrmServiceClient` compatibility as the first external contract for the emulator.

The first compatibility slice will:

- keep the existing connection-string injection pattern
- host an organization service endpoint under `/org`
- use a shared in-memory metadata and record core
- support a narrow `account`-only slice first
- keep Web API as a secondary compatibility surface over the same application flow

We will not introduce an emulator-specific client library as the primary path for the first slice.

## Rationale

- It proves local-dev value against the applications we actually want to unblock.
- It preserves real application bootstrap instead of forcing a test-only code path.
- It keeps the architecture honest by requiring hosted compatibility, not just in-proc substitution.
- It still allows broader external-tool compatibility to be layered in later.

## Consequences

- The hosted Xrm/SOAP surface is a first-class protocol adapter.
- Acceptance tests must include the real `CrmServiceClient` package.
- The `account` slice should stay intentionally narrow until real consumer demand justifies more coverage.
- Web API behavior should continue to share the same application and persistence core rather than diverging into a separate implementation.
