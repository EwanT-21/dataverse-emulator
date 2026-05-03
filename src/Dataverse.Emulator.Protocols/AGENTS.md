# AGENTS.md — Protocols

Compatibility surfaces. Translates external contracts into the shared application/domain flow and maps results back. **Xrm/SOAP is the primary surface; Web API is secondary** (ADR-011).

## Endpoints

| Surface | Path |
|---|---|
| Xrm/SOAP (CoreWCF) | `/org/XRMServices/2011/Organization.svc` |
| Web API | `/api/data/v9.2/` |

## Rules

- **Narrow request-oriented slices.** New Xrm behavior arrives as a `<Operation>XrmRequestHandler` (e.g. `CreateXrmRequestHandler`, `RetrieveMultipleXrmRequestHandler`) — never a monolithic service class.
- **Translators convert SDK types → domain query model**, not into application-layer reimplementations:
  - `DataverseXrmQueryExpressionTranslator` → `RecordQuery`
  - `DataverseXrmLinkedQueryTranslator` (in `Xrm/Queries/`) → `LinkedRecordQuery`
  - `DataverseXrmFetchExpressionTranslator` → `RecordQuery` or `LinkedRecordQuery`
  - `DataverseXrmQueryByAttributeTranslator` → `RecordQuery`
- **Errors via `ErrorOr<T>`** — never throw for user-driven failures. Map `ErrorOr` to SDK faults (Xrm) or JSON errors (Web API) through `Common/DataverseProtocolErrorMapper.cs` and `Xrm/DataverseXrmErrors.cs`.
- **Pre-fetch all required `TableDefinition`s in callers** before invoking translators that need column metadata for value conversion. See `Operations/DataverseXrmRecordOperations.cs` `RetrieveMultipleFetchExpressionAsync` for the pattern (linked-table definitions are pre-loaded via `GetTableDefinitionQuery` before calling `TranslateLinked`).
- **Entity ↔ record mapping** through `DataverseXrmEntityMapper.cs`. Don't hand-roll entity construction in handlers.
- **Service registration** via `DataverseXrmProtocolExtensions.AddDataverseEmulatorXrmProtocol()` for Xrm and the `WebApi/` extensions for Web API.

## Layout (`Xrm/`)

| Folder | Owns |
|---|---|
| `Requests/` | Per-message handlers (`<Op>XrmRequestHandler`) |
| `Operations/` | Operation orchestrators (`DataverseXrmRecordOperations`, `DataverseXrmMetadataOperations`) called by handlers |
| `Queries/` | Query translators that need scope tracking (linked queries) |
| `Metadata/` | Entity/attribute/relationship metadata mappers |
| `MetadataTemplates/` | Static metadata shapes the emulator returns |
| `Execution/` | Request-dispatch infrastructure (`DataverseXrmOrganizationRequestDispatcher`) |
| `Runtime/` | Compatibility settings (organization version, language packs) |
| `Tracing/` | `DataverseXrmRequestTraceStore` and trace recording |

## Translator → execution flow

```
SDK request (QueryExpression/FetchExpression/QueryByAttribute)
  → translator (this project)
  → RecordQuery/LinkedRecordQuery (Domain)
  → mediator.Send(ListRowsQuery / ListLinkedRowsQuery) (Application)
  → repository (Persistence)
  → results back through ErrorOr → SDK response
```

The protocol layer **describes** the query in shared terms; it does **not own** the semantics for evaluating it (ADR-013).

## Drift signals

- Comparing values, joining rows, sorting result sets, or paging directly inside an adapter — those are domain services
- Replicating validation that the application/domain already performs
- Throwing exceptions for user-driven failures (return `ErrorOr<T>` and map at the boundary)
- Cross-entity logic in a translator without going through `LinkedRecordQuery`
- A growing monolith of "dispatcher does everything" — split into per-message handlers
- Web API reaching directly into `Persistence` instead of going through `Application` mediator commands
- Adding new SDK message support without a corresponding integration test in `tests/Dataverse.Emulator.IntegrationTests/Xrm/` or compatibility test in `AspireTests`
