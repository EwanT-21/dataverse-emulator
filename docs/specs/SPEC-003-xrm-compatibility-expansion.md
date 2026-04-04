# SPEC-003: Xrm Compatibility Expansion

- Status: Planned
- Date: 2026-04-04

## Summary

The next Xrm-focused delivery slice should deepen compatibility for real local-development applications before the project broadens into more general protocol coverage.

The key principle is demand-driven expansion: support only the metadata, messages, and query features required by real consumer apps that are being validated against the emulator.

This spec assumes the project remains Xrm/C# first and Aspire-friendly. It does not treat broader connector compatibility as the near-term target.

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

### QueryExpression Expansion

- Add support for more condition operators where needed by real apps.
- Add paging support through `PageInfo`.
- Expand sorting and filter behavior while continuing to translate through the shared `RecordQuery` model when practical.

### Metadata-Oriented SDK Reads

- Add targeted metadata request support for the current table slice where local apps need it.
- Keep metadata expansion bounded to the local-emulator scenario rather than broad platform parity.

### Additional Tables

- Add more tables only when a target local workflow or compatibility test requires them.
- Keep each added table covered by seeded metadata and hosted end-to-end tests.

## Constraints

- Xrm remains the primary compatibility surface.
- Web API growth should stay aligned with the same shared application and persistence behavior.
- New coverage should be backed by Aspire-hosted compatibility tests.
- The emulator should continue to behave like a local development dependency, not a full Dataverse clone.
- Power BI or Power Automate scenarios should not drive this spec unless they emerge from a concrete local developer workflow we intentionally choose to support.

## Out Of Scope

- blanket support for all `OrganizationRequest` messages
- full QueryExpression parity
- FetchXML as part of this spec
- full metadata parity across the Dataverse platform
- relationship-heavy behavior unless directly required by a target application
- broad connector-oriented behavior that does not materially improve the local C# developer experience

## Acceptance Signals

- a real target application or harness can run locally with broader Xrm behavior than the current `account` CRUD/query slice
- newly supported requests and query features are covered by hosted end-to-end tests
- unsupported requests continue to fail explicitly and predictably
