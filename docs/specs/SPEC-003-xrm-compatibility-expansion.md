# SPEC-003: Xrm Compatibility Expansion

- Status: In Progress
- Date: 2026-05-03

## Summary

The next Xrm-focused delivery slice should deepen compatibility for real local-development applications before the project broadens into more general protocol coverage.

The key principle is demand-driven expansion: support only the metadata, messages, and query features required by real consumer apps that are being validated against the emulator.

This spec assumes the project remains Xrm/C# first and Aspire-friendly. It does not treat broader connector compatibility as the near-term target.

## Boundary Expectations

- The Xrm layer may translate `QueryExpression`, `FetchExpression`, and request DTOs into shared query or command models.
- Transport-agnostic query semantics such as join evaluation, scoped filtering, sorting, and paging should live in the shared core, not in the Xrm adapter.
- Application handlers and services should orchestrate repository access and use-case flow.
- Protocol adapters should stay focused on translation, fault mapping, and contract shaping.
- Domain-owned smart enums and query types should remain the shared vocabulary after translation rather than Xrm-specific equivalents leaking inward.
- Expected unsupported or invalid outcomes should stay on the shared `ErrorOr` path until the Xrm edge maps them into SDK-style faults.
- FluentValidation should guard application request boundaries for new Xrm slices, while semantic invariants still belong to the domain model and domain services.

## Goals

- Broaden Xrm compatibility without breaking the current shared-core architecture.
- Expand `Execute` coverage based on observed application demand.
- Improve `QueryExpression` support for real local-development scenarios.
- Add metadata read behavior needed by applications that inspect table shape at startup or runtime.
- Keep the project useful as a local emulator dependency for C# developers.

## In Scope

### Additional Xrm Message Coverage

- Add more `OrganizationRequest` handling where it is required by real target applications.
- Prefer concrete, tested request coverage over broad speculative message support.
- Keep unsupported messages faulting clearly rather than silently approximating behavior.
- `ExecuteMultipleRequest` support for batching currently implemented request slices is now part of the hosted Xrm surface.
- `ExecuteMultipleRequest` fault shaping now stays inside the batch response envelope on supported batched request paths and is covered through direct integration tests.
- `ExecuteTransactionRequest` is now part of the implemented Xrm slice for atomic request batching over the currently supported child request surface.
- `UpsertRequest` on the primary-id path is now part of the hosted Xrm surface, while alternate-key upsert remains explicitly unsupported.
- `RetrieveVersionRequest` is now part of the hosted Xrm surface for basic client version reads.
- `RetrieveAvailableLanguagesRequest` and `RetrieveDeprovisionedLanguagesRequest` are now part of the hosted Xrm surface for bounded local language-catalog reads.
- `RetrieveProvisionedLanguagesRequest` is now part of the hosted Xrm surface for bounded local language metadata reads.
- `RetrieveInstalledLanguagePackVersionRequest` and `RetrieveProvisionedLanguagePackVersionRequest` are now part of the hosted Xrm surface for bounded local language-pack version reads.
- `RetrieveInstalledLanguagePacksRequest` and `RetrieveOrganizationInfoRequest` are now part of the hosted Xrm surface for bounded startup-oriented organization reads.
- `RetrieveMetadataChangesRequest` is now part of the implemented metadata-read slice for bounded startup and schema-introspection flows.

### QueryByAttribute Expansion

- `QueryByAttribute` is now part of the supported local-dev query surface.
- The implementation translates through the same shared filtering, sorting, top, and paging semantics already used by `QueryExpression`.
- `QueryByAttribute` support now covers both direct `RetrieveMultiple(QueryBase)` usage and execute-wrapped request paths.

### QueryExpression Expansion

- Add support for more condition operators where needed by real apps.
- `PageInfo` paging is now part of the implemented Xrm slice and should remain covered by hosted compatibility tests.
- Expand sorting and filter behavior while continuing to translate through the shared `RecordQuery` model when practical.
- Grouped `AND` / `OR` filters and a first common set of condition operators are now part of the implemented slice.
- A first top-level `LinkEntity` slice is now part of the implemented Xrm surface for the seeded `account` / `contact` relationship and now executes through shared-core linked-query semantics rather than protocol-owned evaluation logic.
- Inner-join `LinkCriteria` on the current top-level `LinkEntity` slice should remain supported through the shared linked-query model.
- Bounded `LeftOuter` joins are now part of the implemented relational slice for local apps that need root-row preservation without a linked match.
- Nested `LinkEntity` translation is now part of the implemented slice where it still converges on shared linked-query semantics.

### FetchXML Expansion

- Add `RetrieveMultiple(FetchExpression)` support only where it can translate into the shared query model cleanly.
- Keep the first FetchXML slice narrow and table-scoped.
- Prefer explicit faults for joins, aggregates, aliases, and broader platform behavior the emulator does not yet implement.
- A bounded FetchXML `link-entity` projection slice is now part of the implemented surface and reuses the same shared linked-query semantics used for `QueryExpression`.
- `link-entity` filters and `link-entity` ordering remain explicit future work until a real local app requires them.

### Metadata-Oriented SDK Reads

- Add targeted metadata request support for the current table slice where local apps need it.
- Keep metadata expansion bounded to the local-emulator scenario rather than broad platform parity.
- Metadata-id selectors for `RetrieveEntity`, `RetrieveAttribute`, and `RetrieveRelationship` are part of the supported metadata surface and should remain covered.
- Relationship-aware `RetrieveAllEntities` reads are part of the supported metadata surface for the current seeded relationship slice.
- `RetrieveMetadataChangesRequest` should project bounded entity, attribute, and relationship metadata without introducing SDK contracts into the shared core.

### Relationship Expansion

- Add bounded relationship behavior only when real local apps need it.
- Prefer seeded lookup relationships backed by the shared metadata model over speculative general relationship emulation.
- Keep relationship semantics transport-agnostic after translation so Xrm support continues to reuse the shared core.

### Additional Tables

- Add more tables only when a target local workflow or compatibility test requires them.
- Keep each added table covered by seeded metadata and hosted end-to-end tests.

## Constraints

- Xrm remains the primary compatibility surface.
- Web API growth should stay aligned with the same shared application and persistence behavior.
- New coverage should be backed by Aspire-hosted compatibility tests.
- The emulator should continue to behave like a local development dependency, not a full Dataverse clone.
- Power BI or Power Automate scenarios should not drive this spec unless they emerge from a concrete local developer workflow we intentionally choose to support.
- New Xrm breadth should deepen the shared core where necessary, not create a second evaluator inside the protocol layer.

## Out Of Scope

- blanket support for all `OrganizationRequest` messages
- full QueryExpression parity
- full metadata parity across the Dataverse platform
- relationship-heavy behavior unless directly required by a target application
- broad connector-oriented behavior that does not materially improve the local C# developer experience

## Acceptance Signals

- a real target application or harness can run locally with broader Xrm behavior than the current `account` CRUD/query slice
- newly supported requests and query features are covered by hosted end-to-end tests
- unsupported requests continue to fail explicitly and predictably
- real local-app request traces can show which Xrm messages were served or rejected during a run

## Current Compatibility Contracts Under Test

Supported and currently covered by green integration tests:

- metadata-id selectors for `RetrieveEntity`, `RetrieveAttribute`, and `RetrieveRelationship`
- relationship-aware `RetrieveAllEntities` reads for the seeded `account` / `contact` path
- bounded `RetrieveMetadataChangesRequest` entity, attribute, and relationship metadata snapshots
- explicit faults for unsupported `RetrieveMetadataChanges` metadata selectors and nested attribute/relationship criteria
- direct request-dispatch execution for `RetrieveMultipleRequest` over `QueryExpression` and one-table `FetchExpression`
- `QueryByAttribute` translation and execution through record operations and the public organization-service surface
- direct runtime request execution for version, installed-language-pack, and organization-info reads
- multi-target `Associate` / `Disassociate` behavior on the seeded lookup relationship
- `ExecuteMultipleRequest` per-item fault capture that stays inside the batch response envelope on supported batched request paths
- `ExecuteTransactionRequest` atomic commit, rollback, nested-batch rejection, and fault-index shaping
- primary-id-addressed `UpsertRequest` flows where the primary id is supplied as an attribute rather than through `Entity.Id`
- supported inner `LinkEntity` link-criteria evaluation through the shared linked-query path
- bounded `LeftOuter` and nested `LinkEntity` support through shared linked-query semantics
- bounded FetchXML `link-entity` projection through the shared linked-query path

Current high-value gaps that remain intentionally unsupported or demand-driven:

- broader `RetrieveMetadataChangesRequest` selector and criteria semantics beyond the current bounded startup-oriented slice
- `FetchExpression` `link-entity` filtering and ordering beyond the currently implemented projection slice
- additional `OrganizationRequest` coverage only when a real local app or harness run demonstrates the need

## Current Progress Notes

- Xrm request handling now has a cleaner enhancement seam through small request-oriented handlers instead of a single growing dispatch implementation.
- `RetrieveMultiple(QueryExpression)` paging through `PageInfo` is implemented and verified through the real `CrmServiceClient` Aspire harness.
- `RetrieveMultiple(QueryExpression)` now supports grouped filters and common operators including `NotEqual`, `Null`, `NotNull`, `Like`, `BeginsWith`, `EndsWith`, range comparisons, and `In`.
- `RetrieveMultiple(QueryExpression)` now supports top-level inner `LinkEntity` joins with aliased linked-column projection for the seeded relational slice.
- `RetrieveMultiple(QueryByAttribute)` now translates through the shared single-table query path, including ordering, top, and paging behavior.
- `RetrieveMultiple(QueryExpression)` now supports bounded `LeftOuter` joins and nested `LinkEntity` translation through the shared linked-query path.
- The current linked-query slice now translates in the Xrm adapter, orchestrates in the application layer, and executes its transport-agnostic semantics through shared domain services.
- Single-table and linked-query execution now share domain-owned value comparison, sorting, and continuation paging semantics, reducing evaluator drift between Xrm-facing query shapes.
- `RetrieveMultiple(FetchExpression)` now supports a bounded one-table slice for projection, nested filters, common operators, ordering, and paging through the shared query engine.
- `RetrieveMultiple(FetchExpression)` now also supports bounded `link-entity` projection over the seeded relational slice through the shared linked-query path.
- `ExecuteMultipleRequest` is now implemented for successful batching of the request slices the emulator already supports, is verified through the real `CrmServiceClient` harness, and now has direct integration coverage for per-item fault capture.
- `ExecuteTransactionRequest` is now implemented for atomic batching of the currently supported child request slices, including rollback on child faults, nested-batch rejection, and `FaultedRequestIndex` shaping.
- `UpsertRequest` is now implemented for primary-id addressed create-or-update flows and is verified through the real `CrmServiceClient` harness, while alternate-key upsert continues to fault clearly.
- `RetrieveVersionRequest` is now implemented and verified through the real `CrmServiceClient` harness.
- `RetrieveAvailableLanguagesRequest` and `RetrieveDeprovisionedLanguagesRequest` are now implemented and verified through the real `CrmServiceClient` harness.
- `RetrieveProvisionedLanguagesRequest` is now implemented and verified through the real `CrmServiceClient` harness.
- `RetrieveInstalledLanguagePackVersionRequest` and `RetrieveProvisionedLanguagePackVersionRequest` are now implemented and verified through the real `CrmServiceClient` harness.
- `RetrieveInstalledLanguagePacksRequest` and `RetrieveOrganizationInfoRequest` are now implemented and verified through the real `CrmServiceClient` harness.
- Metadata-oriented Xrm reads for the seeded table slice are now implemented through `RetrieveEntity`, `RetrieveAttribute`, `RetrieveRelationship`, and `RetrieveAllEntities`, including metadata-id selector coverage for the bounded seeded slice.
- `RetrieveMetadataChangesRequest` is now implemented for bounded entity, attribute, and relationship metadata snapshots, while broader nested query criteria remain explicitly unsupported until a real app needs them.
- A first bounded lookup-relationship slice is now implemented through `Associate`, `Disassociate`, `AssociateRequest`, `DisassociateRequest`, and `RetrieveRelationshipRequest` for the seeded `contact_customer_accounts` relationship.
- Multi-target lookup association behavior, direct request-dispatch coverage, runtime-request execution, primary-id-only upsert, and supported inner-join link criteria are now covered through direct integration tests in addition to the hosted harness.
- Xrm request trace capture is now implemented so local runs can inspect which direct operations and `Execute` requests were served or rejected.
- The next likely Xrm expansion points are broader `RetrieveMetadataChanges` selectors or nested criteria only when a real local app needs them, additional `OrganizationRequest` messages only when demand appears in traces or harness runs, and only broadening FetchXML `link-entity` filters or orders when a real local app needs them, while continuing to deepen shared execution helpers only where they stay transport-agnostic.
