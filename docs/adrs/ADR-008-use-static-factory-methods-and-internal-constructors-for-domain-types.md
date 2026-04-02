# ADR-008: Use Static Factory Methods And Internal Constructors For Domain Types

- Status: Accepted
- Date: 2026-04-02

## Context

The early domain model relied on public constructors. That is simple at first, but it pushes validation and object-shape rules into constructor guards and makes call sites noisy as the model grows.

The project owner prefers a domain style where object creation is explicit and intention-revealing:

- public construction happens through named factory methods
- constructors stay closed to the assembly
- tests can still inspect internals when necessary

## Decision

We will prefer:

- internal constructors for domain types
- public static `Create(...)` methods for external construction

Factories will return `ErrorOr<T>` for expected creation failures.

## Rationale

- Named factories communicate intent more clearly than wide constructors.
- Internal constructors reduce accidental bypassing of validation rules.
- `ErrorOr<T>` fits expected domain-construction failures better than constructor exceptions.

## Consequences

- External callers create domain objects through `Create(...)` methods.
- Tests use the same public factories by default and may access internals through friend assemblies when needed.
- Domain methods that evolve existing valid state may still use internal constructors directly when no new external validation boundary is being crossed.
