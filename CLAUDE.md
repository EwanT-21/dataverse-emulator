# CLAUDE.md

Guidance for Claude Code working in this repository.

Routing and delegation policy lives in root `AGENTS.md`. Before planning ambiguous or cross-layer work, read `docs/engineering/AGENT_GUIDE.md`, then operate from the narrowest directory that contains the task. Project subagents in `.claude/agents/` mirror that shared taxonomy; they do not replace it.

## Commands

Solution: `Dataverse.Emulator.slnx`. Use `dotnet restore|build|test|format` against it. Test a single project with `dotnet test tests/<Name>` (`Domain.Tests`, `IntegrationTests`, `AspireTests`, `CrmServiceClientHarness`).

Run: `dotnet run --project src/Dataverse.Emulator.AppHost` (Aspire-hosted, default) or `src/Dataverse.Emulator.Host` (bare). Admin endpoints: `/_emulator/v1/{reset,snapshot,traces/xrm}`. SOAP at `/org/XRMServices/2011/Organization.svc`; Web API at `/api/data/v9.2/`.

## Architecture

Local Dataverse environment that real `CrmServiceClient` apps connect to — not a mock. Xrm/C# is the primary compatibility surface; Web API is secondary. `net10.0` everywhere except the `net48` harness.

Dependency direction (inner layers know nothing of outer):

```
AppHost → Host → { Protocols, Persistence.InMemory, Application → Domain }
```

| Project | Owns |
|---|---|
| `Domain` | Transport-agnostic semantics: tables, columns, records, query models, filter/sort/paging, relationship definitions, invariants. No HTTP/SOAP/SDK/storage. |
| `Application` | Mediator handlers, CRUD orchestration, seeding, association, repository abstractions, FluentValidation. |
| `Protocols` | Xrm/SOAP (CoreWCF) + Web API adapters, request translation, error mapping. Narrow `<Operation>XrmRequestHandler` slices — never a monolithic service. |
| `Persistence.InMemory` | In-memory metadata/record storage, seeded state, query execution. |
| `Host` | Composition root, health/admin endpoints, protocol registration, startup seeding. |
| `AppHost` | Aspire orchestration; public API: `AddDataverseEmulator()`, `WithSeedScenario()`, connection-string shaping. |

If behavior can be described without HTTP/SOAP/SDK/repository terms, it belongs in `Domain`.

### Patterns (enforced by ADRs in `docs/adrs/`)

- **`ErrorOr<T>`** — expected failures flow through all layers; adapters map to SDK faults or HTTP errors. Don't throw exceptions for user-driven failures.
- **Mediator** (source-generated) with `ValidationBehavior<TRequest, TResponse>` pipeline. Handlers registered via `Application.AssemblyMarker`.
- **FluentValidation** at application boundary only; domain invariants live in domain factories/aggregates/services, not only in validators.
- **Smart enums** (`Ardalis.SmartEnum`) for emulator language (operators, sort direction, required levels) — domain only.
- **Specifications** (`Ardalis.Specification`) for repository queries.

### Drift signals

Protocol adapters comparing/joining/sorting directly · transport DTOs leaking into domain query models · exceptions instead of `ErrorOr` · invariants only in validators.

### Current slice

Seeded `account` + `contact` with lookup `contact.parentcustomerid → account.accountid` (schema `contact_customer_accounts`), in-memory only. Covers CRUD, Associate/Disassociate, Upsert, RetrieveMultiple (QueryExpression/FetchXml/QueryByAttribute) with paging via continuation tokens, AND/OR filters, common operators, inner+left-outer `LinkEntity` with aliased projection, metadata reads, and `ExecuteMultipleRequest`. Expand by real client paths, not broad platform parity — resist Power BI/Power Automate scope unless a concrete local workflow demands it.

## Conventions

- Nullable refs + implicit usings enabled; 4-space indent; sealed by default.
- Names: `<Operation>XrmRequestHandler`, `<CommandOrQuery>Validator`, `<Subject>Tests`.
- Commits: one capitalized imperative sentence ending with a period (e.g. `Add upsert support.`).
- Background context lives in `docs/architecture.md`, `docs/adrs/`, `docs/specs/`, `AGENTS.md`, and `docs/engineering/AGENT_GUIDE.md`.
