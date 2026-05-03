# AGENTS.md — Application

Mediator orchestration, request validation, repository abstractions. Decides *how* requests move through the emulator; `Domain` decides *what is valid*.

## Rules

- **Mediator handlers** registered via `Application.AssemblyMarker` assembly scan. Pipeline runs `ValidationBehavior<TRequest, TResponse>` (ADR-010).
- **Validators are optional per request** (ADR-012). Add one only when a request needs boundary validation. Domain invariants stay in domain factories/aggregates/services, never replaced by validators (ADR-004).
- **All handlers return `ErrorOr<T>`** so transports can map results to either SDK faults or HTTP errors uniformly.
- **Repository contracts live in `Abstractions/`** (ADR-005). Handlers depend on these abstractions; concrete implementations are in `Persistence.*`.
- **Service registration** through `ServiceCollectionExtensions.AddDataverseEmulatorApplication()`. Don't register from elsewhere.

## Naming conventions

| Shape | Pattern |
|---|---|
| Command | `<Action>Row(s)Command` (e.g. `CreateRowCommand`, `AssociateRowsCommand`) |
| Command handler | `<Command>Handler` |
| Command validator | `<Command>Validator` |
| Query | `<Get\|List><Subject>Query` (e.g. `GetRowByIdQuery`, `ListLinkedRowsQuery`) |
| Query handler | `<Query>Handler` (note: queries drop the `Query` suffix on the handler — see `GetRowByIdHandler.cs`) |
| Query validator | `<Query>Validator` |

When adding a slice, follow the four-file rhythm in `Records/`: `XCommand.cs`, `XCommandHandler.cs`, `XCommandValidator.cs` (optional), and the matching test in `tests/`.

## Layout

| Folder | Owns |
|---|---|
| `Abstractions/` | Repository contracts implemented by `Persistence.*` |
| `Behaviors/` | `ValidationBehavior<,>` and any future Mediator pipeline behaviors |
| `Common/` | `ValidationExtensions`, `ValidationResponseFactory` (FluentValidation → `ErrorOr` mapping) |
| `Metadata/` | Table/relationship lookup queries and handlers |
| `Records/` | CRUD commands, list/linked-list queries, association services |
| `Seeding/` | `SeedScenario`, `SeedScenarioExecutor`, `SeedScenarioSnapshotService`, baseline state services |

## Drift signals

- Domain evaluation rules (filter comparison, sort comparison, join logic) implemented inside a handler or service here
- Validators replacing what should be a domain invariant (ADR-004)
- Handlers reaching into `Persistence.*` concrete types instead of through `Abstractions/`
- Throwing exceptions for user-driven failures instead of returning `ErrorOr<T>`
- New behaviors registered ad-hoc in `Host` instead of through this project's `ServiceCollectionExtensions`
