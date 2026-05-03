# AGENTS.md — tests

Four projects, layered by scope. Pick the narrowest project that proves the behavior.

## Test projects

| Project | Scope | When to use |
|---|---|---|
| `Dataverse.Emulator.Domain.Tests` | Pure domain (no DI) | Invariants, factory `Create()` validation, smart enum behavior, query model construction |
| `Dataverse.Emulator.IntegrationTests` | In-memory DI graph (Application + Persistence + Protocols) | Translator correctness, protocol-layer flow, end-to-end record/query operations through the mediator |
| `Dataverse.Emulator.AspireTests` | Hosted via Aspire with real `CrmServiceClient` over SOAP | Cross-surface compatibility (Xrm + Web API), workflow tests (reset/snapshot), real-client bootstrap |
| `Dataverse.Emulator.CrmServiceClientHarness` | `net48` console harness | Legacy framework compatibility — exercise from .NET Framework rather than .NET 10 |

## Decision rules

- A new domain rule → test in `Domain.Tests`. Fast and free of DI.
- A new SDK message handler → integration test in `IntegrationTests/Xrm/` driving `DataverseXrmRecordOperations` or `DataverseXrmOrganizationRequestDispatcher`. Add an Aspire E2E only if the message has cross-surface implications.
- A new query translator path (QueryExpression / FetchXML / QueryByAttribute) → integration test in `IntegrationTests/Protocols/`.
- A new application command/query → integration test in `IntegrationTests/Application/`. Add a domain test if the command exposes a new domain rule.
- Web API parity behavior → `AspireTests/WebApi/` or `AspireTests/CrossSurface/`.

## Conventions

- xUnit `[Fact]` and `[Theory]`. Naming: `<Subject>Tests`. Use `<Subject>TddTests` while red-driving a slice; rename when stable.
- Test method names describe behavior in a sentence: `Method_Does_Thing_When_Condition`.
- Keep test names behavior-oriented, not implementation-oriented.

## Test infrastructure (`IntegrationTests/Support/`)

- `XrmProtocolTestContext.CreateAsync(scenario)` — boots the in-memory DI graph and seeds it. Use `await using var context = ...` and access `context.RecordOperations`, `context.MetadataOperations`, `context.OrganizationService`, etc.
- `ProtocolTestMetadataFactory` — preset `account`/`contact` table definitions and record builders. Use `CreateDefaultXrmScenario(...records)` to bootstrap.

## Drift signals

- Mocking the database (use the in-memory provider — that *is* the test seam)
- Adding a test that needs production state spread across multiple projects (suggests refactoring is needed first)
- Aspire E2E for behavior already proven at the integration level (slow, redundant)
- Tests asserting on internal types — assert on observable behavior at the operation surface
