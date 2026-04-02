# Dataverse Emulator

Local-first emulation of Microsoft Dataverse for development workflows that currently depend on Dataverse, XRM clients, and D365-oriented data connectors.

The long-term goal is to let a local application connect to an emulator exactly as it would to a real Dataverse environment, while giving developers faster inner-loop feedback, cleaner test environments, and repeatable seeded state from source control.

## Current Status

This repository is intentionally at the scaffold stage. The solution structure is in place, the host boots, and the documentation defines the architectural seams. Protocol behavior, metadata execution, and connector compatibility will be added incrementally.

## Design Priorities

- Compatibility before completeness. Support the most common Dataverse integration paths first.
- Local developer experience first. Fast boot, deterministic state, and easy resets matter.
- Keep the emulator core independent from transport protocols.
- Start with in-memory persistence and add durable providers later.
- Prefer vertical slices that prove real connector compatibility early.

## Solution Layout

- `src/Dataverse.Emulator.Host`
  - ASP.NET Core entry point, configuration, diagnostics, and future protocol hosting.
- `src/Dataverse.Emulator.Domain`
  - Core model for metadata, records, relationships, and invariants.
- `src/Dataverse.Emulator.Application`
  - Use cases, message orchestration, query execution, seeding, and emulator workflows.
- `src/Dataverse.Emulator.Protocols`
  - Protocol adapters for Web API, XRM-facing behavior, auth compatibility, and request translation.
- `src/Dataverse.Emulator.Persistence.InMemory`
  - Default local storage provider for fast development and test execution.
- `tests/Dataverse.Emulator.Domain.Tests`
  - Unit tests around domain rules and invariants.
- `tests/Dataverse.Emulator.IntegrationTests`
  - Host- and protocol-level tests that validate end-to-end compatibility slices.

## Suggested First Vertical Slice

1. Model table metadata, attributes, primary keys, and simple seeded records.
2. Implement `Create`, `Retrieve`, `Update`, and `RetrieveMultiple` in the application layer.
3. Expose those operations through a thin Web API compatibility surface.
4. Validate the slice against one real client path before broadening support.

## Docs

- Architecture: `docs/architecture.md`
- Roadmap: `docs/roadmap.md`

## Run

```bash
dotnet run --project src/Dataverse.Emulator.Host
```
