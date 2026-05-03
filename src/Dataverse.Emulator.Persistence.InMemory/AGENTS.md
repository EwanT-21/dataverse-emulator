# AGENTS.md — Persistence.InMemory

First storage provider. Implements repository abstractions defined in `Application` over an in-memory dataset.

## Rules

- Implement repository interfaces from `Application/Abstractions/`. Do not invent storage interfaces here.
- Query execution operates against in-memory state populated via the seed scenario flow.
- All public surface is registered through `ServiceCollectionExtensions.AddDataverseEmulatorInMemoryPersistence()`.
- Errors that callers can recover from go through `ErrorOr<T>`.

## Layout

- `InMemoryRepository.cs` — central repository implementation
- `Metadata/` — table-definition lookup over in-memory metadata cache
- `Records/` — row storage and query execution

## Drift signals

- Storage details (collections, locks, indexes) leaking through repository return types
- Owning protocol-shaped errors (those belong in `Protocols/`)
- Encoding query semantics here instead of delegating to `Domain` services
- Adding emulator administration concerns (snapshots/reset) — those belong in `Host/Application` seeding

## Future durable providers

A future provider must respect this same boundary: implement the same `Application/Abstractions/` contracts, return domain types, surface failures via `ErrorOr<T>`. The `Application` layer should not need to change to swap providers.
