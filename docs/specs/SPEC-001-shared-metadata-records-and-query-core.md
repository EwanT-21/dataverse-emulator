# SPEC-001: Shared Metadata, Records, And Query Core

- Status: Implemented
- Date: 2026-04-04

## Summary

The emulator provides a shared, transport-agnostic core for Dataverse-like metadata, records, and query orchestration.

This is the foundation that both the hosted Xrm surface and the secondary Web API surface rely on.

## Goals

- Model table and column metadata in a way that is independent of HTTP and SOAP.
- Support seeded local state for repeatable development and test workflows.
- Provide shared CRUD and list/query behavior through the application layer.
- Keep persistence replaceable so later providers can reuse the same core abstractions.
- Return expected failures through a shared error model rather than transport-specific behavior.

## Implemented Behavior

- Metadata model for tables and columns, including:
  - logical names
  - entity set names
  - primary id and primary name attributes
  - required levels
  - attribute types
- Record model for entity rows and record values.
- Shared query concepts including:
  - selected columns
  - conditions
  - sorting
  - top
  - paging inputs and continuation tokens
- Application-layer commands and queries for:
  - create row
  - retrieve row by id
  - update row
  - delete row
  - list rows
  - retrieve table definitions
- In-memory persistence for metadata and records.
- Seeded startup state for the current `account` table slice.
- Shared `ErrorOr`-based result flow and validation/domain-service enforcement.

## Current Constraints

- The implemented slice is intentionally centered on one table: `account`.
- Relationship modeling is not part of the current shared-core slice.
- Durable persistence is not part of the current shared-core slice.
- Query breadth is limited by the compatibility slices built on top of this core.

## Out Of Scope

- Protocol-specific request parsing.
- SOAP or Web API response formatting.
- Client bootstrap behavior.
- Full Dataverse metadata surface area.

## Verification

This slice is currently proven by:

- domain tests for metadata and record invariants
- integration tests for application orchestration
- hosted compatibility tests that exercise the same core through Xrm and Web API adapters
