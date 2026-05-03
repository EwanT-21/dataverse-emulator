# AGENTS.md — AppHost

Aspire orchestration. Default developer entry point (ADR-006). Public API surface for consumer applications.

## Public API (consumer-facing — change with care)

| Method | Purpose |
|---|---|
| `AddDataverseEmulator()` | Add the emulator project resource and connection-string resource |
| `WithSeedScenario(name)` | Choose startup baseline (e.g. `default-seed`, `empty`) |
| `WithSnapshotFile(path)` | Boot from a snapshot document |
| `WithOrganizationVersion(version)` | Set the reported organization version |
| `WithXrmTraceLimit(count)` | Configure Xrm trace ring-buffer size |
| `WithDataverseConnectionString(envVar)` | Bind the generated `dataverse` connection string into a consumer's environment variable |

## Rules

- Emits two Aspire resources: project `dataverse-emulator` and connection-string `dataverse`. Consumers chain `WithDataverseConnectionString(...)` to bind into their own service.
- Public extension methods live in `DataverseEmulatorAppHostExtensions.cs`. The composition itself is in `AppHost.cs`.
- All resource shaping uses Aspire's fluent builder pattern — return `IResourceBuilder<T>` for chaining.
- Do not duplicate environment-variable knowledge from `Host/`. Set them via Aspire shaping methods, then `Host` reads `DataverseEmulatorHostEnvironmentVariables`.

## Discipline: keep the public API narrow

- A `With*` helper exists when a real consumer workflow needed it. New helpers require a justifying workflow — not anticipated demand.
- Renaming or removing an existing helper is a breaking change for consumers. Prefer adding a sibling helper and deprecating later.

## Drift signals

- Orchestration logic creeping into `Host` (it should be triggered from here)
- Helpers added speculatively without a real consumer driving the shape
- Direct dependencies on `Application`/`Protocols` types in the public surface — keep it Aspire-only
