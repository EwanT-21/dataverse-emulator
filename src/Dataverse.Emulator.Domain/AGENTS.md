# AGENTS.md — Domain

Transport-agnostic emulator semantics. If a behavior can be described without HTTP, SOAP, SDK DTOs, or storage, it belongs here.

## Rules

- **Sealed records with internal constructors + static `Create()` factories** returning `ErrorOr<T>` (ADR-008). Construction outside this assembly must go through the factory so invariants are enforced.
- **Smart enums** (`Ardalis.SmartEnum`) describe emulator language: `ConditionOperator`, `FilterOperator`, `SortDirection`, `RequiredLevel`, `LinkedRecordJoinType` (ADR-001).
- **`ErrorOr<T>`** for expected failures (ADR-002, ADR-009). Exceptions are reserved for misuse and infrastructure faults — never for user-driven outcomes.
- **`AggregateRoot`** base class lives in `Common/`. Use it for entities with internal Guid identity (ADR-007).
- **Transport-agnostic query semantics** (ADR-013): `RecordQuery`, `LinkedRecordQuery`, `QueryFilter`, `QueryCondition`, `QuerySort`, `PageRequest`. Filtering/sorting/joining/projection rules live in `Services/`.

## Layout

| Folder | Owns |
|---|---|
| `Common/` | `AggregateRoot`, `DomainErrors` (centralized error factory) |
| `Metadata/` | `TableDefinition`, `ColumnDefinition`, `LookupRelationshipDefinition`, `AttributeType`, `RequiredLevel` |
| `Queries/` | Query model records and smart enums (operators, direction) |
| `Records/` | `EntityRecord`, `RecordValues` and row invariants |
| `Services/` | Domain services: query value evaluation, single-table + linked-query execution, paging, validation, lookup resolution |

## Drift signals

- Importing `Microsoft.Xrm.Sdk.*`, ASP.NET Core, CoreWCF, or anything from `Persistence.*` / `Protocols.*`
- Throwing exceptions for user-driven outcomes instead of returning `ErrorOr<T>`
- Public constructors on records — must be internal, with a `Create()` factory
- Query semantics that name a transport (e.g. "FetchXML aggregate") — domain stays at the level of `RecordQuery`/`LinkedRecordQuery`
- Validators in `Application` enforcing an invariant that has no domain rule backing it (the rule should live here)
