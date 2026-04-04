# ADR-010: Run FluentValidation Through Mediator Pipeline Behaviors

- Status: Accepted
- Date: 2026-04-02

Note: the "missing validators are a configuration error" consequence recorded below is superseded by `ADR-012`. The pipeline behavior remains in place, but validator presence is now optional rather than mandatory.

## Context

The application layer already uses `FluentValidation` for command and query validation, but the first implementation injected validators directly into handler constructors and invoked them inside each handler.

That approach works, but it duplicates the same validation flow across handlers and mixes cross-cutting validation concerns into orchestration code.

The project is now using `Mediator.SourceGenerator`, and the upstream Mediator guidance includes validation as a pipeline behavior.

## Decision

We will move `FluentValidation` execution out of handlers and into a generic `Mediator` pipeline behavior.

Handlers will no longer inject validators directly.

Instead:

- validators remain registered in DI
- `AddMediator(...)` registers a validation pipeline behavior
- the behavior runs all validators for the incoming message before invoking the handler
- validation failures are converted into `ErrorOr<T>` responses

We also treat missing validators for mediator messages as a configuration error and fail fast.

## Rationale

- Keeps handlers focused on orchestration and domain decisions.
- Aligns with the Mediator repository's recommended validation pattern.
- Centralizes validation behavior in one place instead of repeating it per handler.
- Preserves the project's `ErrorOr`-first approach for expected validation failures.

## Consequences

- Every mediator command/query in the application layer is expected to have at least one validator.
- Adding a new handler without a validator will fail loudly during execution instead of silently skipping validation.
- Domain-specific validation that depends on loaded aggregates or richer business rules may still remain in handlers or domain services until it is intentionally moved into richer validators.
