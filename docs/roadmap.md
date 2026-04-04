# Roadmap

## Phase 0: Foundation

Completed:

- establish solution boundaries
- create runnable Host and AppHost entry points
- document architecture and contribution direction

## Phase 1: Shared Metadata And Records Core

Completed:

- define table and column metadata
- support seeded state
- build in-memory persistence abstractions
- implement shared CRUD and query orchestration with Mediator
- add validation and domain-service checks around the shared core

## Phase 2: First Hosted Compatibility Slice

Completed:

- host a real `CrmServiceClient` bootstrap path at `/org`
- support `account` CRUD plus `RetrieveMultiple(QueryExpression)`
- expose matching Web API CRUD on `/api/data/v9.2/accounts`
- serve metadata for both the Xrm and Web API slices
- prove the slice with Aspire-hosted end-to-end tests

## Phase 3: Xrm-First Local Workflow Expansion

In progress:

- add deterministic reset support for the default seeded state

Next likely steps:

- add more `Execute` message coverage where real apps actually need it
- expand QueryExpression support beyond top-level `AND` + `Equal`
- add more tables only when a target local-dev scenario requires them
- expand reset, seeding, and snapshot-oriented local workflows beyond the current default reset endpoint
- keep Web API growth aligned with the same shared-core capabilities

## Phase 4: Aspire Packaging And Emulator Ergonomics

Later work:

- make the emulator easier to compose into local Aspire-based app environments
- add richer snapshot import/export and durable local persistence options
- improve environment-shaping features where they materially help local developers

## Phase 5: Broader Compatibility Exploration

Explicitly later and not a current commitment:

- broader Web API parity
- FetchXML
- Power BI-oriented compatibility
- Power Automate or flow-oriented compatibility
- broader external-tool support beyond the local C# developer story

## Contribution Rule Of Thumb

Every roadmap item should answer one of these questions clearly:

- does it improve compatibility with a real local .NET/Xrm client path?
- does it improve deterministic local development workflows?
- does it preserve the shared-core architecture instead of adding duplicate behavior?
