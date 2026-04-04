# Dataverse Emulator

Local-first emulation of Microsoft Dataverse for C# and Xrm-oriented development workflows.

The primary goal is to let an existing .NET application keep its current connection-string bootstrap pattern, point at a locally hosted emulator, and run against a shared in-memory Dataverse-like core. Aspire is the default way to host that local environment.

Think "local emulator for Dataverse-backed .NET apps" more than "full local clone of every Dataverse consumer."

## Product Positioning

The project is currently aimed at:

- C# developers who want to run Dataverse-backed apps locally
- Aspire-hosted local environments
- Xrm/SDK compatibility first

The project is not currently centered on:

- Power BI connector compatibility
- Power Automate or flow automation compatibility
- broad external-tool parity across the Dataverse ecosystem

Those scenarios may matter later, but they are not driving the current roadmap.

## Current Status

The repository now implements a real first compatibility slice:

- Shared in-memory core for metadata, records, and query orchestration.
- Seeded `account` metadata with entity set `accounts`.
- Hosted Xrm/C# compatibility for the real legacy `CrmServiceClient`.
- Supported C# operations:
  - `Create(Entity)`
  - `Retrieve(string, Guid, ColumnSet)`
  - `Update(Entity)`
  - `Delete(string, Guid)`
  - `RetrieveMultiple(QueryExpression)`
- Secondary Dataverse Web API support on `/api/data/v9.2/accounts`.
- Shared error model mapped into:
  - SDK-style faults for Xrm/C#
  - Dataverse-style HTTP errors for Web API
- Aspire-driven end-to-end tests, including a reusable `net48` harness that uses the real `CrmServiceClient`.

## Current Scope

The emulator is intentionally narrow right now:

- One table: `account`
- In-memory storage only
- QueryExpression support limited to:
  - top-level `AND`
  - `ConditionOperator.Equal`
  - `OrderExpression`
  - `TopCount`
- Web API support limited to matching CRUD plus metadata for the current table slice

Not implemented yet:

- multi-table support
- FetchXML
- broader `Execute` message coverage
- relationship modeling and traversal
- auth emulation beyond permissive local bootstrap
- durable persistence providers

## Compatibility Tiers

- Primary: hosted Xrm/C# compatibility for existing .NET applications using the current connection-string pattern.
- Secondary: Web API compatibility where it supports the same local workflows, tests, and debugging experience.
- Deferred: broader connector compatibility such as Power BI, Power Automate, or other external-tool scenarios.

## Design Priorities

- Preserve existing app bootstrap patterns wherever possible.
- Optimize first for the local C# developer workflow.
- Prove real client compatibility before broadening feature scope.
- Keep the emulator core transport-agnostic.
- Optimize for fast local startup, deterministic state, and repeatable tests.
- Use Aspire as the default local orchestration path.
- Keep Web API as a supporting compatibility surface unless a real local workflow requires more.

## Solution Layout

- `src/Dataverse.Emulator.AppHost`
  - Default local entry point for Aspire orchestration.
- `src/Dataverse.Emulator.Host`
  - Emulator web process, health endpoints, protocol registration, and seeded startup.
- `src/Dataverse.Emulator.Domain`
  - Core language for tables, columns, rows, and query concepts.
- `src/Dataverse.Emulator.Application`
  - Mediator handlers, validation behavior, seeding, and orchestration.
- `src/Dataverse.Emulator.Protocols`
  - Hosted Xrm/SOAP adapter, Web API adapter, protocol translation, and error mapping.
- `src/Dataverse.Emulator.Persistence.InMemory`
  - Default local metadata and record storage provider.
- `tests/Dataverse.Emulator.Domain.Tests`
  - Domain invariants and validation tests.
- `tests/Dataverse.Emulator.IntegrationTests`
  - Open-box integration tests and protocol translation tests.
- `tests/Dataverse.Emulator.AspireTests`
  - Aspire-hosted end-to-end tests across Web API and Xrm/C#.
- `tests/Dataverse.Emulator.CrmServiceClientHarness`
  - `net48` harness that uses the real `CrmServiceClient` package in end-to-end tests.

## Local Run

Default local orchestration:

```bash
dotnet run --project src/Dataverse.Emulator.AppHost
```

Direct host only:

```bash
dotnet run --project src/Dataverse.Emulator.Host
```

Local emulator connection string for the current slice:

```text
AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local
```

## Tests

```bash
dotnet test Dataverse.Emulator.slnx
```

## Docs

- Architecture: `docs/architecture.md`
- Roadmap: `docs/roadmap.md`
- ADRs: `docs/adrs`
- Specs: `docs/specs`

Key ADRs for the current shape:

- `ADR-006` for Aspire-first local orchestration.
- `ADR-011` for hosted `CrmServiceClient` compatibility as the first external contract.
- `ADR-012` for optional validators in the Mediator pipeline.
