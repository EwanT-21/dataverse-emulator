# Dataverse Emulator

Local-first emulation of Microsoft Dataverse for development workflows that currently depend on Dataverse, XRM clients, and D365-oriented data connectors.

The long-term goal is to let a local application connect to an emulator exactly as it would to a real Dataverse environment, while giving developers faster inner-loop feedback, cleaner test environments, and repeatable seeded state from source control.

## Current Status

The repository is past the initial scaffold stage and now includes the first internal application slice for metadata and record orchestration.

Implemented so far:

- Domain models for tables, columns, rows, and transport-agnostic query concepts.
- Static factory-based domain creation with internal constructors and `ErrorOr`-based expected failures.
- `Ardalis.Specification`-aligned generic repositories for aggregate retrieval and mutation.
- In-memory persistence with a separate `RecordQuery` path for Dataverse-style list semantics.
- `Mediator.SourceGenerator`-backed commands and queries with `FluentValidation` enforced through a Mediator pipeline behavior.
- Seed scenario execution and test coverage for domain and integration-level behavior.

What is still missing:

- Dataverse-compatible HTTP and XRM protocol surfaces.
- Connector-oriented query translation such as OData, QueryExpression, and FetchXML.
- Durable persistence providers beyond the in-memory implementation.

## Design Priorities

- Compatibility before completeness. Support the most common Dataverse integration paths first.
- Local developer experience first. Fast boot, deterministic state, and easy resets matter.
- Keep the emulator core independent from transport protocols.
- Start with in-memory persistence and add durable providers later.
- Prefer vertical slices that prove real connector compatibility early.

## Solution Layout

- `src/Dataverse.Emulator.Host`
  - ASP.NET Core entry point, configuration, diagnostics, and future protocol hosting.
- `src/Dataverse.Emulator.Domain`
  - Core model for metadata, records, relationships, and invariants.
- `src/Dataverse.Emulator.Application`
  - Use cases, Mediator handlers, validation behaviors, query execution, seeding, and emulator workflows.
- `src/Dataverse.Emulator.Protocols`
  - Protocol adapters for Web API, XRM-facing behavior, auth compatibility, and request translation.
- `src/Dataverse.Emulator.Persistence.InMemory`
  - Default local storage provider with generic aggregate repositories and a specialized record-query path.
- `tests/Dataverse.Emulator.Domain.Tests`
  - Unit tests around domain rules and invariants.
- `tests/Dataverse.Emulator.IntegrationTests`
  - Host- and protocol-level tests that validate end-to-end compatibility slices.

## Current Slice

The current implementation supports the internal domain and application workflow for:

1. Defining table metadata and seeded state.
2. Creating, retrieving, updating, deleting, and listing rows through Mediator-backed application handlers.
3. Validating request shape through a shared validation pipeline and enforcing richer semantics in domain services.
4. Running the same slice against the in-memory persistence provider in tests.

## Suggested Next Slice

1. Expose the existing CRUD/query application flow through a thin Dataverse Web API-compatible adapter.
2. Map `ErrorOr` outcomes onto transport-specific responses.
3. Validate the slice against one real client path before broadening support.

## Docs

- Architecture: `docs/architecture.md`
- Roadmap: `docs/roadmap.md`
- ADRs: `docs/adrs`

Notable implemented ADRs include:

- `ADR-005` for `Ardalis.Specification` with generic repositories.
- `ADR-007` for the aggregate root base class.
- `ADR-008` for static factories and internal constructors.
- `ADR-009` for exception boundaries versus expected `ErrorOr` failures.
- `ADR-010` for Mediator-based validation pipeline behavior.

## Run

```bash
dotnet run --project src/Dataverse.Emulator.Host
```

## Test

```bash
dotnet test Dataverse.Emulator.slnx
```
