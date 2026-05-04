# Dataverse Emulator

Dataverse Emulator is a local-first emulator for Microsoft Dataverse focused on .NET applications that already use Xrm/SDK connection-string bootstrap patterns.

The project hosts a compatible organization service endpoint and a secondary Web API surface over a shared in-memory core for metadata, records, query semantics, and local workflow tooling.

The project is intentionally demand-driven. It aims to unblock real local development workflows, not to approximate every Dataverse feature or every connector ecosystem.

## Goals

- Preserve existing bootstrap patterns for Dataverse-backed .NET applications.
- Provide deterministic local state, fast startup, and repeatable tests.
- Keep emulator semantics transport-agnostic so Xrm and Web API share the same core.
- Expand compatibility only when a real local workflow needs it.

## Non-Goals

- Blanket Dataverse parity across the full platform surface.
- Broad Power BI, Power Automate, or connector-oriented compatibility.
- Silent approximation of unsupported platform behavior.
- Durable production-style persistence in the current phase.

## Quickstart

Run the emulator from source (.NET 10 SDK required) with the default seed (`account` + `contact` + `contact_customer_accounts` lookup):

```bash
dotnet run --project src/Dataverse.Emulator.AppHost
```

The Aspire dashboard prints the bound port. The Xrm/SOAP endpoint is at `http://localhost:{port}/org` and the Web API at `http://localhost:{port}/api/data/v9.2/`.

Once a tagged release is published, the same emulator is also available as a multi-arch container image:

```bash
docker run --rm -p 8080:8080 ghcr.io/ewant-21/dataverse-emulator-host:latest
# Xrm/SOAP: http://localhost:8080/org
# Web API:  http://localhost:8080/api/data/v9.2/
```

Wire the emulator into your own Aspire AppHost:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var dataverse = builder.AddDataverseEmulator();

builder.AddProject<Projects.MyApp>("my-app")
    .WithDataverseConnectionString(dataverse, "CrmConnectionString");

builder.Build().Run();
```

Consume the generated connection string from a `CrmServiceClient` app exactly as you would today:

```csharp
var connectionString = Environment.GetEnvironmentVariable("CrmConnectionString");
using var client = new CrmServiceClient(connectionString);

var account = new Entity("account") { ["name"] = "Local Test" };
var id = client.Create(account);
```

The emulator preserves the existing `CrmServiceClient` bootstrap path, so no app code changes are needed beyond pointing at the local connection string.

## Current Compatibility

### Seeded Scope

- Seeded tables:
  - `account`
  - `contact`
- Seeded lookup relationship:
  - `contact.parentcustomerid -> account.accountid`
  - schema name: `contact_customer_accounts`
- Named local baselines:
  - `default-seed`
  - `empty`
- Persistence:
  - in-memory only

### Supported Xrm/C# Surface

Direct organization service operations:

- `Create(Entity)`
- `Retrieve(string, Guid, ColumnSet)`
- `Update(Entity)`
- `Delete(string, Guid)`
- `Associate(string, Guid, Relationship, EntityReferenceCollection)`
- `Disassociate(string, Guid, Relationship, EntityReferenceCollection)`
- `RetrieveMultiple(QueryExpression)`
- `RetrieveMultiple(QueryByAttribute)`
- `RetrieveMultiple(FetchExpression)`

Supported `Execute` message slices:

- batching and command-style requests:
  - `ExecuteMultipleRequest`
  - `ExecuteTransactionRequest`
  - `UpsertRequest` on the primary-id path
- runtime and organization reads:
  - `WhoAmIRequest`
  - `RetrieveCurrentOrganizationRequest`
  - `RetrieveVersionRequest`
  - `RetrieveAvailableLanguagesRequest`
  - `RetrieveDeprovisionedLanguagesRequest`
  - `RetrieveProvisionedLanguagesRequest`
  - `RetrieveInstalledLanguagePackVersionRequest`
  - `RetrieveProvisionedLanguagePackVersionRequest`
  - `RetrieveInstalledLanguagePacksRequest`
  - `RetrieveOrganizationInfoRequest`
- metadata reads:
  - `RetrieveEntityRequest`
  - `RetrieveAttributeRequest`
  - `RetrieveAllEntitiesRequest`
  - `RetrieveRelationshipRequest`
  - `RetrieveMetadataChangesRequest`
- relationship requests:
  - `AssociateRequest`
  - `DisassociateRequest`

Supported query breadth:

- `QueryExpression`
  - grouped `AND` / `OR` filters
  - `Equal`
  - `NotEqual`
  - `Null` / `NotNull`
  - `Like`
  - `BeginsWith` / `EndsWith`
  - `GreaterThan` / `GreaterThanOrEqual`
  - `LessThan` / `LessThanOrEqual`
  - `In`
  - `OrderExpression`
  - `TopCount`
  - `PageInfo` paging
  - inner and bounded `LeftOuter` `LinkEntity` joins across the seeded tables
  - nested `LinkEntity` translation where it still converges on the shared linked-query model
  - aliased linked-column projection
- `QueryByAttribute`
  - translation through the shared single-table query path
  - ordering, top, and paging
- `FetchExpression`
  - one-table queries over the seeded tables
  - bounded `link-entity` projection, filtering, and ordering across the seeded relational slice
  - root `entityname` filters and ordering over linked aliases where they still converge on the shared linked-query model
  - nested filters, ordering, and paging through the shared query engine

### Secondary Web API Surface

- `/api/data/v9.2/accounts`
- `/api/data/v9.2/contacts`
- shared error semantics aligned with the Xrm surface through the same application/core flow

### Local Workflow Support

- reset to the configured startup baseline:
  - `POST /_emulator/v1/reset`
- reset to a named baseline:
  - `POST /_emulator/v1/reset?scenario=empty`
- export the current in-memory state:
  - `GET /_emulator/v1/snapshot`
- import a snapshot and replace the current in-memory state:
  - `POST /_emulator/v1/snapshot`
- inspect captured Xrm request traces:
  - `GET /_emulator/v1/traces/xrm`
- clear captured Xrm request traces:
  - `DELETE /_emulator/v1/traces/xrm`

### Optional Compatibility Telemetry

- Compatibility telemetry is disabled by default. Configure `DATAVERSE_EMULATOR_TELEMETRY_ENDPOINT` (or call `WithCompatibilityTelemetryEndpoint(...)` from the AppHost) to opt in; both the endpoint and the enable flag must be set for delivery.
- Events are intentionally narrow: protocol, source, emulator error code, capability kind, capability key, timestamp, emulator version, and runtime version.
- Request payloads, record data, entity IDs, connection strings, raw trace messages, and raw custom request names are not sent.
- Set `DATAVERSE_EMULATOR_TELEMETRY_ENABLED=false` to override and disable delivery even when an endpoint is configured.

## Intentional Limits

The emulator is intentionally narrow in the current phase:

- the seeded `account` / `contact` model is the primary compatibility slice
- metadata breadth is bounded to the current seeded model and the currently supported startup-oriented requests
- `QueryExpression` does not implement aggregates, `Distinct`, or total-count paging
- `FetchExpression` does not implement aggregates, `distinct`, attribute aliases, `valueof` conditions, or broader `link-entity` shapes beyond the current bounded shared linked-query slice
- alternate-key upsert is explicitly unsupported
- broader `RetrieveMetadataChanges` selectors, metadata properties, and condition operators beyond the current bounded startup-oriented slice are explicitly unsupported
- unsupported features are expected to fault clearly rather than degrade silently

## Asking For Compatibility

The emulator only adds compatibility for messages a real local workflow needs. The intended loop is:

1. Run your app against the emulator. Unsupported requests fault clearly with an emulator-specific error code rather than degrading silently.
2. Inspect what the app actually attempted:

   ```bash
   curl http://localhost:{port}/_emulator/v1/traces/xrm
   ```

   Each trace entry names the SDK message the emulator received and whether it was handled, refused, or unrecognized.
3. Open an issue with the trace excerpt and a short description of the local workflow that needed the message. Concrete traces are weighted over speculative parity asks.

This is the same loop the maintainers use to choose what lands next, and it is the fastest way to influence the roadmap.

## Architecture At A Glance

- `src/Dataverse.Emulator.Domain`
  - transport-agnostic metadata, records, query language, and query semantics
- `src/Dataverse.Emulator.Application`
  - Mediator handlers, orchestration, validation, seeding, and cross-aggregate workflows
- `src/Dataverse.Emulator.Protocols`
  - Xrm/SOAP and Web API adapters, translation, request handlers, and error mapping
- `src/Dataverse.Emulator.Persistence.InMemory`
  - the default storage provider and query access over the in-memory dataset
- `src/Dataverse.Emulator.Host`
  - the emulator process, admin endpoints, protocol registration, and startup composition
- `src/Dataverse.Emulator.AppHost`
  - Aspire packaging, connection-string shaping, and reusable local orchestration helpers

The detailed architecture narrative lives in [docs/architecture.md](docs/architecture.md). Durable design decisions live under [docs/adrs](docs/adrs). Demand-driven compatibility slices are tracked under [docs/specs](docs/specs).

## Verification Strategy

The project uses layered verification rather than one large end-to-end test bucket:

- `tests/Dataverse.Emulator.Domain.Tests`
  - pure domain invariants and execution semantics
- `tests/Dataverse.Emulator.IntegrationTests`
  - translation, orchestration, fault shaping, and bounded compatibility contracts
- `tests/Dataverse.Emulator.AspireTests`
  - hosted end-to-end verification across the emulator process and Aspire wiring
- `tests/Dataverse.Emulator.CrmServiceClientHarness`
  - `net48` harness that exercises the real legacy `CrmServiceClient`

This split keeps compatibility changes verifiable at the narrowest useful level while still preserving a real hosted-client proof path.

## Local Run

Default local orchestration:

```bash
dotnet run --project src/Dataverse.Emulator.AppHost
```

Direct host only:

```bash
dotnet run --project src/Dataverse.Emulator.Host
```

Run the full test suite:

```bash
dotnet test Dataverse.Emulator.slnx
```

Local emulator connection string for the current slice:

```text
AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local
```

Basic Aspire usage:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var dataverse = builder.AddDataverseEmulator()
    .WithSeedScenario("empty")
    .WithOrganizationVersion("9.2.0.0")
    .WithXrmTraceLimit(100);

var app = builder.Build();
app.Run();
```

Custom seed state via snapshot:

The two built-in scenarios are `default-seed` (the seeded `account` + `contact` model) and `empty`. To start from any other shape, build it once and replay it from a snapshot file.

```bash
# 1. start the emulator with the default scenario
dotnet run --project src/Dataverse.Emulator.AppHost

# 2. mutate the in-memory state from your app, scripts, or Web API directly

# 3. export the resulting state to a JSON snapshot file
curl http://localhost:{port}/_emulator/v1/snapshot > seed.json
```

Commit `seed.json` next to the AppHost project, mark it as copied to output, and replay it on every startup:

```xml
<!-- in your AppHost .csproj -->
<ItemGroup>
  <None Update="seed.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

```csharp
var dataverse = builder.AddDataverseEmulator()
    .WithSnapshotFile(Path.Combine(AppContext.BaseDirectory, "seed.json"));
```

The same file format is accepted by `POST /_emulator/v1/snapshot` for runtime resets. This keeps custom seed shapes outside the project's code surface while still giving consuming repos a deterministic baseline.

Telemetry configuration:

```csharp
var dataverse = builder.AddDataverseEmulator()
    .WithCompatibilityTelemetryEndpoint("https://telemetry.example.test/v1/events");
```

Legacy executable resource wiring:

```csharp
var dataverse = builder.AddDataverseEmulator();

builder.AddExecutable("legacy-xrm-app", @"C:\apps\LegacyXrmApp.exe", @"C:\apps")
    .WithDataverseConnectionString(dataverse, "CrmConnectionString");
```

## Documentation Map

- [docs/architecture.md](docs/architecture.md)
  - boundaries, dependency direction, and execution flow
- [docs/roadmap.md](docs/roadmap.md)
  - staged product and compatibility roadmap
- [docs/specs](docs/specs)
  - bounded delivery slices and acceptance signals
- [docs/adrs](docs/adrs)
  - durable architectural decisions
- [AGENTS.md](AGENTS.md)
  - repository-wide contributor workflow and provider-neutral agent instructions
- [docs/engineering/AGENT_GUIDE.md](docs/engineering/AGENT_GUIDE.md)
  - routing, scoping, and delegation guidance for human-supervised agent workflows

Key ADRs for the current project shape:

- `ADR-006` for Aspire-first local orchestration
- `ADR-011` for hosted `CrmServiceClient` compatibility as the first external contract
- `ADR-013` for transport-agnostic query semantics in the domain
- `ADR-014` for demand-driven compatibility over speculative platform parity
- `ADR-015` for pairing real-client hosted tests with direct compatibility tests
- `ADR-016` for provider-neutral agent guidance with thin provider-specific overlays
