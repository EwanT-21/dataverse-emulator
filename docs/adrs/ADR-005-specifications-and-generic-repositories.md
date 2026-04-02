# ADR-005: Use Specifications With Generic Repositories For Aggregate Retrieval, Not For Transport Query Semantics

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator will need a growing set of read and existence checks for aggregate roots such as:

- table by logical name
- table by entity set name
- row by id
- row by alternate key
- row existence checks for validation and business rules

These checks can become repetitive if every variant expands the repository surface area. The project owner also has prior experience with generic repositories and specifications, and there is value in sharing business predicates between validation and retrieval paths.

At the same time, the emulator must support Dataverse-native query semantics for external consumers:

- OData query options
- QueryExpression
- FetchXML
- paging and continuation tokens
- projection and protocol-specific shaping

These are not just aggregate retrieval concerns.

## Decision

We will adopt `Ardalis.Specification` and generic repositories for aggregate-centric retrieval and existence checks.

We will not use Ardalis specifications as the primary language for connector-facing Dataverse query semantics.

Instead, we will use a hybrid approach:

- specifications for aggregate loading, lookups, uniqueness checks, and reusable business predicates
- emulator-native query models such as `RecordQuery` for transport/query semantics exposed by Dataverse-compatible protocols

## Rationale

- This captures the reuse benefits of specifications without forcing OData, QueryExpression, or FetchXML into an aggregate-repository abstraction.
- Domain and application rules can reuse the same predicates for retrieval and pre-insertion/pre-update checks.
- Repository interfaces remain smaller while still expressing meaningful intent.

## Consequences

- Generic repositories are appropriate for aggregate roots and internal lookups.
- Specialized query abstractions remain appropriate for protocol-facing list and search behavior.
- Specifications should remain domain/application meaningful and avoid infrastructure-only concerns such as provider-specific tuning flags.

## Guidance

Good candidates for specifications:

- `TableByLogicalNameSpecification`
- `TableByEntitySetNameSpecification`
- `RecordByIdSpecification`
- `RecordByAlternateKeySpecification`
- `RecordWithColumnValueSpecification`

Poor candidates for specifications:

- direct OData parsing models
- FetchXML as a first-class repository abstraction
- protocol response shaping concerns
