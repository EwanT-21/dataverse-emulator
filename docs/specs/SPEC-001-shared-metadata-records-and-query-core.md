# SPEC-001: Shared Metadata, Records, And Query Core

- Status: Implemented
- Date: 2026-04-05

## Summary

The emulator provides a shared, transport-agnostic core for Dataverse-like metadata, records, and query orchestration.

This is the foundation that both the hosted Xrm surface and the secondary Web API surface rely on.

## Goals

- Model table and column metadata in a way that is independent of HTTP and SOAP.
- Model lookup relationship definitions in a way that is independent of HTTP and SOAP.
- Support seeded local state for repeatable development and test workflows.
- Provide shared CRUD and list/query behavior through the application layer.
- Keep transport-agnostic query semantics in the shared core rather than in protocol adapters.
- Keep persistence replaceable so later providers can reuse the same core abstractions.
- Return expected failures through a shared error model rather than transport-specific behavior.

## Implemented Behavior

- Metadata model for tables and columns, including:
  - logical names
  - entity set names
  - primary id and primary name attributes
  - required levels
  - attribute types
  - lookup target tables and relationship schema names
- Record model for entity rows and record values.
- Shared lookup relationship definitions derived from the metadata model.
- Shared query concepts including:
  - selected columns
  - conditions
  - sorting
  - top
  - paging inputs and continuation tokens
  - linked-table query language for root scope, linked scopes, projection, and paging
- Application-layer commands and queries for:
  - create row
  - retrieve row by id
  - update row
  - delete row
  - list rows
  - list linked rows
  - retrieve table definitions
  - associate related rows through shared lookup relationship definitions
  - disassociate related rows through shared lookup relationship definitions
  - retrieve lookup relationship definitions
- Domain services for:
  - single-table query validation
  - single-table query execution semantics for filtering, sorting, projection, and continuation paging
  - record validation
  - lookup relationship definition discovery and validation against metadata
  - linked-query semantic validation against metadata
  - linked-query execution semantics for join, filter, sort, projection, and paging
- In-memory persistence for metadata and records.
- Seeded startup state for the current `account` and `contact` slice.
- Shared `ErrorOr`-based result flow and validation/domain-service enforcement.

## Boundary Expectations

- Domain owns transport-agnostic query and validation semantics.
- Domain owns transport-agnostic relationship definitions and semantic validation.
- Application owns use-case orchestration and repository composition.
- Protocols translate external contracts into the shared query language and map results out again.
- Persistence providers should reuse the shared-core query semantics where practical instead of re-inventing them per protocol.

## ADR Alignment

- Smart enums and other domain-owned value types should express emulator language in the shared core rather than protocol-specific equivalents.
- `ErrorOr` remains the expected path for validation failures, unknown metadata, unsupported shared semantics, and other non-exceptional outcomes that higher layers must map cleanly.
- FluentValidation applies at the application boundary for request-shape and use-case-entry validation, but it does not replace domain invariants or domain validation services.
- Shared-core behavior should remain explainable without HTTP, SOAP, `QueryExpression`, or `FetchExpression` terminology after translation has occurred.

## Current Constraints

- The implemented slice is intentionally centered on the seeded `account` / `contact` relationship path.
- Relationship modeling is still narrow and only covers the currently seeded lookup workflow.
- Durable persistence is not part of the current shared-core slice.
- Query breadth is still limited by the compatibility slices built on top of this core.
- Single-table and linked-query execution still use different domain executors because their query shapes are different, but they now share the same domain value-evaluation and continuation paging services.

## Out Of Scope

- Protocol-specific request parsing.
- SOAP or Web API response formatting.
- Client bootstrap behavior.
- Full Dataverse metadata surface area.

## Verification

This slice is currently proven by:

- domain tests for metadata and record invariants
- domain tests for single-table query execution semantics
- domain tests for linked-query validation and execution semantics
- integration tests for application orchestration
- hosted compatibility tests that exercise the same core through Xrm and Web API adapters
