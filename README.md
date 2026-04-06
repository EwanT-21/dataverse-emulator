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
- Single-table and linked-query execution now share domain-owned value comparison, sorting, and continuation paging semantics.
- Seeded `account` and `contact` metadata with entity sets `accounts` and `contacts`.
- Local reset workflow that restores the default seeded state.
- Local reset workflow that restores a configured or named baseline state.
- Hosted Xrm/C# compatibility for the real legacy `CrmServiceClient`.
- Supported C# operations:
  - `Create(Entity)`
  - `Retrieve(string, Guid, ColumnSet)`
  - `Update(Entity)`
  - `Delete(string, Guid)`
  - `Associate(string, Guid, Relationship, EntityReferenceCollection)`
  - `Disassociate(string, Guid, Relationship, EntityReferenceCollection)`
  - `UpsertRequest` for primary-id addressed upsert
  - `RetrieveVersionRequest`
  - `RetrieveMultiple(QueryExpression)`
  - `RetrieveMultiple(FetchExpression)`
- Supported QueryExpression breadth:
  - rooted queries over `account` and `contact`
  - grouped `AND` / `OR` filters
  - `Equal`
  - `NotEqual`
  - `Null` / `NotNull`
  - `Like`
  - `BeginsWith` / `EndsWith`
  - `GreaterThan` / `GreaterEqual`
  - `LessThan` / `LessEqual`
  - `In`
  - `OrderExpression`
  - `TopCount`
  - `PageInfo` paging
  - top-level `LinkEntity` inner joins across the seeded tables
  - aliased linked-column projection in `RetrieveMultiple`
- Supported FetchXML breadth:
  - one-table queries over the seeded tables
  - `<attribute>` projection and `<all-attributes />`
  - nested `<filter type='and|or'>`
  - `eq`, `ne`, `null`, `not-null`, `like`, `begins-with`, `ends-with`
  - `gt`, `ge`, `lt`, `le`, `in`
  - `<order>`
  - `count`, `page`, and `paging-cookie`
- Supported Xrm metadata reads:
  - `RetrieveEntity`
  - `RetrieveAttribute`
  - `RetrieveAllEntities`
  - `RetrieveRelationship`
- Supported generic `Execute` coverage:
  - `RetrieveVersionRequest` for client version reads
  - `RetrieveProvisionedLanguagesRequest` for local language metadata reads
  - `UpsertRequest` for create-or-update flows addressed by primary id
  - `ExecuteMultipleRequest` for batching currently supported request slices
  - `AssociateRequest` and `DisassociateRequest` for the seeded lookup relationship
  - `RetrieveRelationshipRequest` for seeded relationship metadata
- Secondary Dataverse Web API support on `/api/data/v9.2/accounts` and `/api/data/v9.2/contacts`.
- Shared error model mapped into:
  - SDK-style faults for Xrm/C#
  - Dataverse-style HTTP errors for Web API
- AppHost packaging for a reusable Aspire emulator resource plus a generated `dataverse` connection string resource.
- AppHost resource shaping methods for seed scenario selection, snapshot-backed startup, organization version configuration, and Xrm trace retention.
- AppHost consumer helper for mapping the generated emulator connection string into a project or executable resource's chosen environment variable.
- Snapshot export and import workflows on `/_emulator/v1/snapshot`.
- Xrm request trace inspection and clear workflows on `/_emulator/v1/traces/xrm`.
- Aspire-driven end-to-end tests, including a reusable `net48` harness that uses the real `CrmServiceClient`.

## Current Scope

The emulator is intentionally narrow right now:

- Two seeded tables: `account` and `contact`
- One seeded lookup relationship: `contact.parentcustomerid -> account.accountid`
  - schema name: `contact_customer_accounts`
- In-memory storage only
- Named seed baselines:
  - `default-seed`
  - `empty`
- QueryExpression support limited to:
  - top-level `LinkEntity` inner joins only
  - no nested `LinkEntity`
  - no left outer joins
  - no aggregates or `Distinct`
  - no total-count paging
- FetchXML support limited to:
  - one-table queries only
  - no `link-entity`
  - no aggregates or `distinct`
  - no aliases
  - no total-count paging
- Metadata reads limited to the current seeded table slice
- Relationship support limited to direct lookup association and metadata for the seeded relationship slice
- Web API support limited to matching CRUD plus metadata for the current seeded tables

Not implemented yet:

- broader multi-table coverage beyond the current seeded relational slice
- alternate-key upsert
- FetchXML joins
- broader `Execute` message coverage beyond the current demand-driven slice
- broader relationship modeling and traversal beyond the current bounded lookup-association slice
- auth emulation beyond permissive local bootstrap
- multiple named seed scenarios
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
  - Hosted Xrm/SOAP adapter, Web API adapter, protocol translation, error mapping, and Xrm request-handler slices.
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

AppHost packaging for the current slice:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDataverseEmulator();

var app = builder.Build();
app.Run();
```

AppHost shaping for local environments:

```csharp
var dataverse = builder.AddDataverseEmulator()
    .WithSeedScenario("empty")
    .WithOrganizationVersion("9.2.0.0")
    .WithXrmTraceLimit(100);
```

Snapshot-backed startup:

```csharp
var dataverse = builder.AddDataverseEmulator()
    .WithSnapshotFile(@"C:\snapshots\baseline.json");
```

Legacy executable resource wiring:

```csharp
var dataverse = builder.AddDataverseEmulator();

builder.AddExecutable("legacy-xrm-app", @"C:\apps\LegacyXrmApp.exe", @"C:\apps")
    .WithDataverseConnectionString(dataverse, "CrmConnectionString");
```

- This is the intended Aspire bridge for legacy `.NET Framework` Xrm apps: they participate as executable resources and receive the emulator connection string through the setting name they already expect.

- The packaged emulator currently exposes:
  - project resource name: `dataverse-emulator`
  - connection string resource name: `dataverse`
- `AddDataverseEmulator()`, `DataverseEmulatorAppHostResource`, `WithDataverseConnectionString(...)`, `WithSeedScenario(...)`, `WithSnapshotFile(...)`, and `WithOrganizationVersion(...)` are public so the packaging seam can later move into a dedicated Aspire Community Toolkit-style extension library cleanly.
- `AddDataverseEmulator()`, `DataverseEmulatorAppHostResource`, `WithDataverseConnectionString(...)`, `WithSeedScenario(...)`, `WithSnapshotFile(...)`, `WithOrganizationVersion(...)`, and `WithXrmTraceLimit(...)` are public so the packaging seam can later move into a dedicated Aspire Community Toolkit-style extension library cleanly.

## Local Workflow Support

- Reset the emulator back to its default seeded state with:
 
```bash
POST /_emulator/v1/reset
```

- Reset the emulator to a named baseline state with:

```bash
POST /_emulator/v1/reset?scenario=empty
```

- Without a query parameter, reset restores the configured startup baseline.
- Export the current in-memory emulator state with:

```bash
GET /_emulator/v1/snapshot
```

- Import a previously exported snapshot and replace the current in-memory state with:

```bash
POST /_emulator/v1/snapshot
```

- Inspect captured Xrm request traces with:

```bash
GET /_emulator/v1/traces/xrm
```

- Clear captured Xrm request traces with:

```bash
DELETE /_emulator/v1/traces/xrm
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
- `ADR-013` for keeping transport-agnostic query semantics in the domain.
