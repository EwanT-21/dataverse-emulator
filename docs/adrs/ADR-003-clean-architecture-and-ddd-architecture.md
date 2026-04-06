# ADR-003: Use Clean Architecture With DDD-Oriented Boundaries

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator is intended to mimic Dataverse semantics while remaining maintainable as an open-source project. The system must support:

- core metadata and record rules
- multiple protocol surfaces such as Web API and XRM
- multiple storage providers, beginning with in-memory persistence
- local developer workflows such as seeding, reset, snapshots, and compatibility testing

A layered monolith would risk mixing protocol details, storage concerns, and emulator semantics. That would make it difficult to evolve the emulator safely as compatibility grows.

## Decision

We will structure the solution around clean architectural boundaries with DDD-oriented responsibilities:

- `Dataverse.Emulator.Domain`
  - metadata, record, relationship, and query concepts
- `Dataverse.Emulator.Application`
  - commands, queries, handlers, validation, orchestration
- `Dataverse.Emulator.Protocols`
  - Web API, XRM, and future connector-facing adapters
- `Dataverse.Emulator.Persistence.*`
  - concrete storage implementations
- `Dataverse.Emulator.Host`
  - composition root and runtime host
- `Dataverse.Emulator.AppHost` and `Dataverse.Emulator.ServiceDefaults`
  - Aspire orchestration and shared service defaults

## Aggregate Guidance

The early aggregate roots are:

- `TableDefinition`
- `EntityRecord`

Cross-aggregate workflows belong in application services and handlers rather than a large environment aggregate.

## Rationale

- The emulator core must stay independent from HTTP, OData, XRM SDK, and storage implementation details.
- Dataverse compatibility work will expand unevenly, so boundaries must support incremental vertical slices.
- DDD-oriented naming makes the model easier to reason about than a generic CRUD abstraction.

## Consequences

- Transport adapters depend on application/domain, not the reverse.
- Persistence implementations depend on application/domain abstractions, not the reverse.
- New compatibility work should generally begin as a domain/application slice before a new protocol adapter is added.
- Transport-agnostic emulator semantics must not settle permanently in protocol adapters just because a protocol introduced them first.
- Lookup relationship definitions and other shared metadata semantics should stay in the shared core, even when the first consuming slice arrives through Xrm.
- Emulator language such as operators, required levels, or sort direction should be expressed with domain-owned types such as smart enums rather than transport-specific constants.
- Expected failures should move through the core as `ErrorOr` results and only be mapped into protocol-specific faults or HTTP responses at the edge.
- FluentValidation should guard Mediator request boundaries, while domain invariants remain enforced in the domain model and domain services.
