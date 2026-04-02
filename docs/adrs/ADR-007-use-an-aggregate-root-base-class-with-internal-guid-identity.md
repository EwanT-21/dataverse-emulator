# ADR-007: Use An Aggregate Root Base Class With Internal Guid Identity

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator now uses generic repositories and specifications for aggregate retrieval. A marker interface is enough when the only need is type grouping, but this codebase also needs a shared aggregate concept that can carry common state and behavior over time.

Two aggregate roots are already present:

- `TableDefinition`
- `EntityRecord`

These roots have different public identities in the ubiquitous language:

- tables are addressed by logical name
- rows are addressed by Dataverse row id

At the same time, the project owner wants aggregate roots aligned on an internal `Guid` identity for consistency and future extensibility.

## Decision

We will replace the aggregate-root marker interface with an abstract `AggregateRoot` base class.

That base class owns an internal `Guid` aggregate identity that is not required to be the same as the aggregate's public Dataverse-facing identifier.

## Rationale

- A base class is a better fit once aggregate roots share state rather than only a marker.
- An internal aggregate identity gives us a stable implementation-level identifier without forcing natural keys and Dataverse-facing ids into one shape.
- This keeps the domain language clean:
  - `TableDefinition.LogicalName` remains the meaningful table identifier
  - `EntityRecord.Id` remains the meaningful Dataverse row identifier

## Consequences

- Repository constraints target `AggregateRoot` rather than a marker interface.
- Aggregate roots can share internal lifecycle or event plumbing later without another broad refactor.
- Tests may need friend-assembly access when validating aggregate internals.
