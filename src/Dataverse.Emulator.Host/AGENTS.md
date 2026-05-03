# AGENTS.md — Host

Web host: composition root, health/admin endpoints, protocol registration, startup seeding.

## Admin endpoints

| Verb | Route | Purpose |
|---|---|---|
| `POST` | `/_emulator/v1/reset` | Reset to default seeded state. Optional `?scenario=<name>` (e.g. `empty`). |
| `GET`  | `/_emulator/v1/snapshot` | Export current in-memory state as a source-controllable document. |
| `POST` | `/_emulator/v1/snapshot` | Import a snapshot document. |
| `GET`  | `/_emulator/v1/traces/xrm` | Inspect captured Xrm request traces. |
| `DELETE` | `/_emulator/v1/traces/xrm` | Clear captured Xrm traces. |

## Rules

- Composition root only — register services, then delegate. No emulator semantics live here.
- Hosted services run startup work: `DefaultSeedHostedService` applies the configured baseline before requests are served.
- Configuration via `DataverseEmulatorHostEnvironmentVariables` (seed scenario, snapshot path, version, trace limits, compatibility telemetry).
- Admin endpoints compose application-owned services (seeding, snapshotting, trace store) — they do not implement the behavior.

## Drift signals

- Emulator semantics (validation, query rules, association logic) implemented inside endpoint handlers
- Endpoints reaching directly into `Persistence.InMemory` instead of going through `Application` services
- New surface added without an environment-variable hook for Aspire to drive
