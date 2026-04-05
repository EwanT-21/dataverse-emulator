# ADR-013: Keep Transport-Agnostic Query Semantics In The Domain

- Status: Accepted
- Date: 2026-04-05

## Context

As Xrm compatibility expanded, the emulator gained more complex query behavior:

- grouped filters
- paging
- linked-table joins
- aliased projection
- FetchXML and `QueryExpression` translation into a shared query flow

The first cleanup pass moved multi-table query execution out of the protocol layer and into the application layer. That was an improvement, but it still left a large amount of transport-agnostic query behavior in an application service.

That behavior was not really about a use case or a protocol. It described emulator semantics:

- how linked rows join
- how scoped filters are evaluated
- how sorting behaves
- how continuation-based paging is applied
- how root and linked projections are shaped before transport mapping

Those rules are part of the emulator's ubiquitous language and should not depend on Xrm, Web API, or any specific storage implementation.

## Decision

We will keep transport-agnostic query semantics in the domain.

That includes:

- shared linked-query models
- validation of linked-query semantics against table metadata
- execution semantics for joins, filter evaluation, sort behavior, projection, and paging

The boundary is:

- `Domain`
  - owns query language and transport-agnostic execution rules
- `Application`
  - owns repository loading, use-case orchestration, Mediator handlers, and composition of domain services
- `Protocols`
  - own translation from external contracts such as `QueryExpression`, `FetchExpression`, and HTTP query input into the shared domain/application flow
- `Persistence`
  - owns data access and storage concerns, but not transport-specific parsing or protocol-specific orchestration

## Rationale

- Query semantics are core emulator behavior, not protocol glue.
- The same semantics should be reusable across Xrm, Web API, FetchXML, and future compatibility paths.
- Keeping them in the domain makes testing easier and keeps application services smaller and more honest.
- This preserves ADR-003's DDD-oriented boundaries as compatibility breadth grows.

## Consequences

- If a rule can be described without mentioning Xrm, HTTP, SOAP, or a specific repository implementation, it probably belongs in the domain.
- Application services may still coordinate multiple repositories and domain services for cross-aggregate workflows.
- Protocol adapters should translate and map responses, not directly execute emulator query behavior.
- Provider-local query evaluators that duplicate domain semantics should trend toward shared domain services over time where practical.

## Enforcement Notes

This ADR should be read as a strong boundary rule, not a soft preference:

- query operators, filter operators, and sort directions used by shared query execution should stay domain-owned rather than being redefined per protocol
- application services in this area should stay orchestration-focused: load metadata, load rows, invoke domain validation and execution, return `ErrorOr`
- protocol layers may reject unsupported transport constructs early, but once a request is translated into the shared query language the evaluation rules belong to the domain
- validation should remain layered:
  - FluentValidation for request-shape concerns at the application boundary
  - domain services and domain rules for semantic correctness inside the shared core

Drift signs for this ADR include large protocol evaluators, application services re-implementing comparison logic, and transport exceptions replacing shared error results for expected query failures.
