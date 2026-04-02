# ADR-002: Use ErrorOr At Application Boundaries

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator needs predictable, composable error handling across several layers:

- command/query validation
- metadata validation
- row validation
- protocol translation into HTTP, Web API, and XRM-compatible error responses

Relying on exceptions for expected failures would make control flow noisy and complicate adapter code. Returning `null` or booleans would lose too much detail for protocol compatibility.

## Decision

We will use `ErrorOr` for application-layer results and for structured domain/application validation feedback.

Expected failures such as:

- table not found
- row not found
- unknown column
- immutable column update
- invalid command/query input

will be returned as structured errors rather than thrown as exceptions.

Exceptions remain appropriate for truly exceptional situations such as programmer errors or infrastructure failures that are not part of normal request handling.

## Rationale

- `ErrorOr` keeps happy-path and expected-failure logic explicit.
- It gives protocol adapters enough structure to map failures onto HTTP and Dataverse/XRM-style responses.
- It aligns well with FluentValidation and future middleware/pipeline behavior.

## Consequences

- Handlers return `ErrorOr<T>` instead of nullable results or exceptions for expected failures.
- Shared error definitions belong in a central domain/application error catalog.
- Protocol adapters will translate `ErrorOr` values into transport-specific responses.
