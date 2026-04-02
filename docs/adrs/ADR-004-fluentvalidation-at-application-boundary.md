# ADR-004: Use FluentValidation For Commands And Queries

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator accepts input from multiple sources:

- internal handlers
- future HTTP/Web API endpoints
- future XRM request adapters
- seeding and test setup paths

These inputs need consistent structural validation before domain logic runs. Structural validation is distinct from domain validation:

- structural validation checks whether input is shaped correctly
- domain validation checks whether the requested change is valid according to metadata and record rules

## Decision

We will use `FluentValidation` for command and query validation in the application layer.

Validation is executed through a shared `Mediator` pipeline behavior before handler orchestration runs.

Examples include:

- required table logical names
- non-empty identifiers
- positive page sizes
- required payload objects

Domain-specific checks such as unknown columns, immutable primary ids, and attribute compatibility remain in domain services and specifications.

## Rationale

- Keeps handler code focused on orchestration.
- Separates request-shape concerns from emulator-semantic concerns.
- Works cleanly with `ErrorOr`.
- Centralizes structural validation instead of repeating the same flow in every handler.

## Consequences

- Each command/query type should normally have a corresponding validator.
- Validation errors are converted into `ErrorOr` values before handler logic proceeds.
- Handlers should not need to inject validators directly when the message is already flowing through Mediator.
- We should avoid pushing Dataverse domain rules down into FluentValidation rules unless they are simple shape checks.
