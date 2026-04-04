# ADR-012: Mediator Validation Remains Centralized But Validators Are Optional

- Status: Accepted
- Date: 2026-04-03

## Context

`ADR-010` moved validation into a shared Mediator pipeline behavior, which remains the right architectural boundary.

However, the original consequence that every Mediator message must have at least one validator turned out to be too strict for the actual development workflow. Some messages are simple enough that a dedicated validator adds ceremony without meaningfully improving correctness.

## Decision

We will keep validation centralized in the Mediator pipeline behavior, but the presence of a validator is optional.

If validators are registered for a message, the pipeline behavior runs them before the handler.

If no validators are registered, the handler executes normally.

## Rationale

- Preserves the clean architectural boundary introduced by `ADR-010`.
- Avoids forcing no-op validators for straightforward requests.
- Keeps validation as a strong development expectation without turning it into mandatory boilerplate.

## Consequences

- Validators are still encouraged for non-trivial commands and queries.
- Missing validators are no longer treated as a configuration error.
- Handlers and domain services remain responsible for the richer invariants that are not captured by lightweight request validators.
