# ADR-006: Use .NET Aspire For Local Orchestration And Service Defaults

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator is specifically a local-development tool. That makes startup experience, diagnostics, health checks, and observability part of the product experience rather than just deployment detail.

The project now includes:

- `Dataverse.Emulator.AppHost`
- `Dataverse.Emulator.ServiceDefaults`

This provides a consistent path for local orchestration and future multi-service growth.

## Decision

We will use .NET Aspire for local orchestration, service discovery, health checks, resilience defaults, and OpenTelemetry wiring.

The `Host` project remains the runnable web service for the emulator itself. The `AppHost` project orchestrates local startup and future companion services.

## Rationale

- Aspire improves the local developer experience, which is central to the emulator's value proposition.
- It provides a consistent place for health, telemetry, and future supporting resources.
- It keeps production/runtime concerns out of the domain and application layers.

## Consequences

- The Host project should use shared service defaults.
- New runtime services should be added to the AppHost rather than stitched together ad hoc.
- Architecture docs and onboarding should treat AppHost as the default developer entry point for local orchestration.
