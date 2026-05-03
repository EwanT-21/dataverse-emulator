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
- expand QueryExpression support to grouped filters and common condition operators
- add a first multi-table `QueryExpression` slice through top-level inner `LinkEntity` joins
- move transport-agnostic linked-query semantics into the shared core and keep application responsible for orchestration
- add demand-driven `Execute` coverage for batched request execution
- add demand-driven `Execute` coverage for `UpsertRequest` on the primary-id path
- add demand-driven `Execute` coverage for `RetrieveVersionRequest`
- add demand-driven `Execute` coverage for `RetrieveProvisionedLanguagesRequest`
- add demand-driven `Execute` coverage for available, deprovisioned, and language-pack version reads used by realistic local language metadata flows
- add demand-driven `Execute` coverage for installed language and organization-info reads used by realistic app-hosted startup flows
- add a first bounded `FetchExpression` slice through the shared query engine
- add `QueryByAttribute` support by translating it through the shared query model
- add `ExecuteTransactionRequest` with application-owned atomic orchestration and rollback semantics
- add bounded `RetrieveMetadataChangesRequest` support for entity, attribute, and relationship metadata snapshots
- align `ExecuteMultipleRequest` fault handling with SDK-style per-item batch responses on supported request paths
- expand QueryExpression support to bounded `LeftOuter` and nested `LinkEntity` semantics where they can still reuse the shared linked-query model
- expand FetchXML support to bounded `link-entity` semantics through the same shared linked-query path
- converge duplicated single-table and linked-query comparison, sorting, and continuation paging rules into shared domain services
- add a first bounded lookup-relationship slice with `Associate`, `Disassociate`, and `RetrieveRelationshipRequest` over the seeded metadata path
- add Xrm request trace capture so real local apps can show which messages the emulator is actually serving or rejecting
- cover metadata-id selectors, relationship-aware metadata reads, direct request-dispatch execution, multi-target relationship operations, primary-id-only upsert, and supported inner-join link criteria through direct integration tests

Next likely steps:

- broaden `RetrieveMetadataChangesRequest` selectors or nested criteria only when a real startup or schema-introspection flow requires them
- add more `OrganizationRequest` coverage only when a traced local app or harness run shows real demand
- broaden FetchXML `link-entity` filters or ordering only when a real local app needs more than projection and shared join semantics
- broaden relationship behavior only when a real local app needs more than the current seeded lookup-association slice
- continue consolidating shared execution building blocks where query shapes can reuse the same domain semantics practically
- add more tables only when a target local-dev scenario requires them
- expand reset, seeding, and snapshot-oriented local workflows beyond the current baseline reset and snapshot endpoints
- keep Web API growth aligned with the same shared-core capabilities

## Phase 4: Aspire Packaging And Emulator Ergonomics

In progress:

- package the emulator as a reusable AppHost resource with a generated connection string
- keep the AppHost packaging surface public so it can later seed a dedicated Aspire toolkit extension
- make the emulator easier to compose into local Aspire-based app environments
- map the generated emulator connection string into a consuming resource's chosen environment variable for project and executable-resource scenarios
- add snapshot export and import endpoints for moving local in-memory state between runs and environments
- add public AppHost shaping methods for startup seed scenario, snapshot-backed startup, and organization version configuration

Next likely steps:

- add more named seed scenarios plus richer snapshot ergonomics
- add durable local persistence options
- add more consumer-oriented AppHost helpers where they materially reduce local setup for real apps
- keep local diagnostics such as Xrm trace capture configurable through the same AppHost packaging seam

## Phase 5: Broader Compatibility Exploration

Explicitly later and not a current commitment:

- broader Web API parity
- Power BI-oriented compatibility
- Power Automate or flow-oriented compatibility
- broader external-tool support beyond the local C# developer story

## Contribution Rule Of Thumb

Every roadmap item should answer one of these questions clearly:

- does it improve compatibility with a real local .NET/Xrm client path?
- does it improve deterministic local development workflows?
- does it preserve the shared-core architecture instead of adding duplicate behavior?
