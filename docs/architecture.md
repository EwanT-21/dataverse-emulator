# Architecture

## Intent

The emulator should behave like a local Dataverse environment that real applications can connect to, not just a fake repository behind a test seam.

The current architecture separates three concerns that will keep evolving at different speeds:

1. Shared Dataverse-like semantics.
2. Client and protocol compatibility.
3. Storage and local developer workflow tooling.

The current product posture is:

- local-development first
- Aspire-hosted by default
- Xrm/C# compatibility first
- Web API compatibility second

In practice, that means the architecture is being optimized for a local C# developer workflow before it is optimized for broad Dataverse ecosystem parity.

## Dependency Direction

The solution is organized so protocol and persistence details depend on the shared core, not the other way around.

```text
AppHost
`- Host
   |- Protocols
   |- Persistence.InMemory
   `- Application
      `- Domain
```

## Project Responsibilities

### `Dataverse.Emulator.Domain`

Owns the transport-agnostic language of the emulator:

- tables and columns
- row identity and invariants
- query concepts that should exist regardless of transport
- validation-oriented domain services

This project stays free from HTTP, SOAP, SDK, and storage concerns.

### `Dataverse.Emulator.Application`

Owns orchestration and use cases:

- Mediator commands, queries, handlers, and pipeline behaviors
- CRUD workflows
- metadata loading and seeded startup behavior
- abstractions for persistence and query execution

If the domain says what is valid, the application layer decides how requests move through the emulator.

### `Dataverse.Emulator.Protocols`

Owns compatibility surfaces:

- hosted Xrm/SOAP compatibility for `CrmServiceClient`
- secondary Dataverse Web API compatibility
- request translation into application commands and queries
- shared transport-level error mapping

This layer should translate external contracts into the shared application flow instead of re-implementing emulator behavior.

Current scope guidance for this layer:

- Xrm/C# is the primary compatibility contract.
- Web API exists to support the same local emulator story.
- Broader connector-oriented behavior should not drive the design unless a concrete local workflow requires it.

### `Dataverse.Emulator.Persistence.InMemory`

Owns the first storage provider:

- in-memory metadata storage
- in-memory record storage
- query execution over the in-memory dataset
- seeded state for local workflows

Later durable providers should be able to follow the same boundary without disturbing the higher layers.

### `Dataverse.Emulator.Host`

Owns the emulator process itself:

- composition root
- health and diagnostic endpoints
- protocol registration
- seeded startup

### `Dataverse.Emulator.AppHost`

Owns local orchestration:

- default developer entry point
- Aspire health wiring
- future companion resources or supporting services

## Current Implemented Slice

The current proven slice is intentionally narrow:

- one table: `account`
- one entity set: `accounts`
- in-memory state only
- hosted organization service bootstrap at `/org/XRMServices/2011/Organization.svc`
- C# operations:
  - `Create`
  - `Retrieve`
  - `Update`
  - `Delete`
  - `RetrieveMultiple(QueryExpression)`
- secondary Web API CRUD on `/api/data/v9.2/accounts`
- shared error model mapped to either SDK faults or HTTP errors

That slice is locked down with:

- domain tests
- integration tests for translation and shared-core behavior
- Aspire-hosted end-to-end tests
- a reusable `net48` harness that uses the real `CrmServiceClient`

## Scope Guidance

Keep expanding by real client paths instead of broad platform imitation.

For now, "real client path" should usually mean a real .NET or Xrm-based local application path, not broad Power Platform ecosystem parity.

The preferred sequence is:

1. choose one concrete client or local workflow path
2. implement only the metadata, query, and message behavior that path needs
3. prove it with hosted compatibility tests
4. broaden outward from a verified slice

That is how the current `account` + `CrmServiceClient` slice should continue to grow into a broader local Dataverse emulator without turning into a shallow protocol collection.

The architecture should continue to resist these scope traps unless they are explicitly justified by a target local workflow:

- designing primarily for Power BI
- designing primarily for Power Automate
- treating the Web API surface as the main product instead of a supporting surface
