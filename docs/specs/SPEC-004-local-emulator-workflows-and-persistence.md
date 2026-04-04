# SPEC-004: Local Emulator Workflows And Persistence

- Status: In Progress
- Date: 2026-04-04

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
- Reset currently reapplies the source-controlled `default-seed` scenario.
- The default seed scenario defines the in-memory `account` metadata slice and clears records back to the initial seeded state.
- Aspire-hosted end-to-end tests verify that:
  - data created during a test run is removed after reset
  - metadata remains available after reset

### Still To Add

- snapshot export and import workflows
- multiple named seed scenarios
- richer AppHost ergonomics for distributing connection information to consuming apps
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

## Out Of Scope

- production deployment design
- cloud-scale clustering behavior
- full auth federation
- features that only make sense for a managed online Dataverse environment

## Acceptance Signals

- consuming teams can start, reset, and reseed the emulator predictably in local environments
- Aspire-hosted workflows become simpler for real applications
- durable persistence can be added without rewriting protocol adapters or core logic
