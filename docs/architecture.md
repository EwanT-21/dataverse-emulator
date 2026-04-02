# Architecture

## Intent

The emulator should behave like a local Dataverse environment, not just a fake repository. That means the architecture needs to separate three concerns that will evolve at different speeds:

1. Core Dataverse semantics.
2. Protocol and client compatibility.
3. Persistence and developer workflow tooling.

## Dependency Direction

The solution is organized so that protocol and persistence details depend on the core, not the other way around.

```text
Host
|- Protocols
|- Persistence.InMemory
`- Application
   `- Domain
```

## Project Responsibilities

### `Dataverse.Emulator.Domain`

Owns the durable language of the emulator:

- Tables, columns, option sets, alternate keys, and relationships.
- Row identity, ownership, concurrency, and invariant rules.
- Query concepts that should exist regardless of transport.
- Domain events or rule violations if you introduce them later.

Keep this project free from HTTP, SDK, and storage concerns.

### `Dataverse.Emulator.Application`

Owns orchestration and use cases:

- CRUD and execute-message workflows.
- Query execution pipeline.
- Metadata loading and seed application.
- Abstractions for persistence, snapshots, and future plugin hooks.

If the domain says what is valid, the application layer decides how requests move through the emulator.

### `Dataverse.Emulator.Protocols`

Owns compatibility surfaces:

- Dataverse Web API and OData translation.
- XRM-facing request mapping.
- Future connector-specific shims where necessary.
- Authentication and connection compatibility adapters.

This layer should translate external requests into application commands and queries instead of re-implementing business behavior.

### `Dataverse.Emulator.Persistence.InMemory`

Owns the first storage provider:

- In-memory record storage.
- Metadata cache and seeded state.
- Reset and snapshot-friendly behavior for local workflows.

Later durable providers, such as file-backed or SQLite-backed storage, can follow the same abstraction boundary without disturbing higher layers.

### `Dataverse.Emulator.Host`

Owns process startup:

- Configuration and composition root.
- Hosted services, diagnostics, and health endpoints.
- Protocol registration.
- Local developer ergonomics such as reset endpoints or seeded startup modes.

## Internal Folders To Introduce Next

As implementation begins, these namespaces will keep the codebase navigable without exploding the number of assemblies too early.

- `Domain/Metadata`
- `Domain/Records`
- `Domain/Relationships`
- `Domain/Queries`
- `Application/Messages`
- `Application/Queries`
- `Application/Seeding`
- `Protocols/WebApi`
- `Protocols/Xrm`
- `Persistence.InMemory/Storage`

## Scope Guidance

Avoid trying to emulate the whole platform at once. A better path is:

1. Pick one connector path.
2. Implement the smallest useful metadata and data behavior required for it.
3. Lock that behavior down with compatibility tests.
4. Expand outward from proven slices.

That strategy will keep the open-source project credible early, because each phase produces a real integration story instead of a broad but shallow codebase.
