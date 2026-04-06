# SPEC-004: Local Emulator Workflows And Persistence

- Status: In Progress
- Date: 2026-04-05

## Summary

Beyond protocol compatibility, the emulator needs product-level local-development workflows: easier environment setup, deterministic resets, shareable seeded state, and eventually persistence options beyond the current in-memory implementation.

This spec captures the local-emulator features that make the project more useful than a thin compatibility shim.

This is also the area where later Aspire packaging or a community toolkit can add a lot of value, without changing the core emulator mission.

## Goals

- Make the emulator easier to adopt in Aspire-hosted local environments.
- Support deterministic reset and seeded-state workflows for teams.
- Add persistence options when developers need state across restarts.
- Keep the local developer loop fast and explicit.

## In Scope

### Aspire Developer Ergonomics

- Make the emulator easy to compose into local AppHost environments.
- Surface connection-string and endpoint information cleanly for consuming apps.
- Keep AppHost as the default local entry point.
- Leave room for later community-toolkit packaging, but keep the core emulator independently useful first.

### Reset And Seeding Workflows

- Support repeatable seeded startup scenarios.
- Add explicit reset and snapshot-friendly workflows for local testing.
- Keep seeded state source-controlled and understandable.

## Current Progress

### Implemented Baseline

- The host exposes `POST /_emulator/v1/reset`.
- Reset now restores either:
  - the configured startup baseline
  - or an explicitly requested named seed scenario through `POST /_emulator/v1/reset?scenario=...`
- The host now also exposes:
  - `GET /_emulator/v1/snapshot`
  - `POST /_emulator/v1/snapshot`
- Snapshot export captures the current in-memory metadata and rows into a stable document model owned by the application seeding layer.
- Snapshot export captures lookup relationship metadata such as lookup target tables and relationship schema names alongside the current in-memory rows.
- Snapshot import restores that document back through the same application-owned seed execution flow rather than bypassing the shared core.
- The default seed scenario defines the in-memory `account` + `contact` metadata slice and clears records back to the initial seeded state.
- Named seed baselines currently include:
  - `default-seed`
  - `empty`
- `AppHost` now packages the emulator through `AddDataverseEmulator()`.
- The current AppHost packaging surface is public so it can later be moved or wrapped in a dedicated Aspire Community Toolkit-style extension package.
- The current AppHost packaging emits:
  - project resource name `dataverse-emulator`
  - generated connection string resource name `dataverse`
- The current AppHost packaging also exposes fluent shaping methods for:
  - startup seed scenario selection
  - snapshot-backed startup
  - emulator organization version configuration
  - Xrm trace retention limit configuration
- The current AppHost packaging can now map the generated emulator connection string into a consuming project's or executable resource's chosen environment variable, which is the intended bridge for legacy Xrm apps participating in Aspire as executable resources.
- The host now also exposes Xrm trace inspection and clear endpoints so local runs can capture the real Xrm message mix a consuming app is generating.
- Aspire-hosted end-to-end tests verify that:
  - data created during a test run is removed after reset
  - metadata remains available after reset
  - snapshot export and import can round-trip runtime-created state
  - captured Xrm traces show both supported and unsupported request flows
  - the emulator connection string can be resolved from the packaged AppHost resource model
  - the packaged AppHost helper can inject the emulator connection string into a consumer-defined environment variable without forcing the default `ConnectionStrings__dataverse` name

### Still To Add

- more named seed scenarios
- more consumer-oriented AppHost helpers beyond the current connection-string injection seam
- durable local persistence providers layered behind the same abstractions

### Persistence Evolution

- Introduce at least one durable local persistence option when needed.
- Preserve the same application and query abstractions used by the in-memory provider.
- Keep persistence choices focused on local development rather than production hosting.

### Environment Shaping

- Support local workflow controls that help developers emulate a known environment state.
- Prefer explicit environment setup over hidden implicit behavior.

## Constraints

- Local development remains the primary goal.
- Faster inner-loop feedback should win over heavyweight infrastructure.
- New local workflows should not bypass the shared application or domain model.
- These workflows should continue to serve the Xrm/C#-first local emulator story before broader ecosystem scenarios.
- Local workflow features such as reset, snapshots, or Aspire packaging should compose existing application and domain behavior rather than re-encoding emulator rules in host-only utilities.

## Out Of Scope

- production deployment design
- cloud-scale clustering behavior
- full auth federation
- features that only make sense for a managed online Dataverse environment

## Acceptance Signals

- consuming teams can start, reset, and reseed the emulator predictably in local environments
- Aspire-hosted workflows become simpler for real applications
- durable persistence can be added without rewriting protocol adapters or core logic
