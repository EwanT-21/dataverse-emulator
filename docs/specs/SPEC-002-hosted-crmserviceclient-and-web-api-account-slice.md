# SPEC-002: Hosted CrmServiceClient And Web API Account Slice

- Status: Implemented
- Date: 2026-04-04

## Summary

The emulator exposes a hosted local-development compatibility slice that supports:

- real legacy `CrmServiceClient` bootstrap through a Dataverse-like organization service endpoint
- matching CRUD over the Dataverse Web API for the same `account` table

Both surfaces run against the same shared in-memory metadata, record, query, and error model.

This spec is primarily about the local C# developer workflow. The Web API surface is supporting this slice, not redefining the project as a broad connector-compatibility effort.

## Goals

- Preserve the existing connection-string bootstrap pattern used by consuming .NET applications.
- Allow a real `CrmServiceClient` to connect to a locally hosted emulator without replacing application business logic.
- Provide a narrow but real Web API slice over the same shared core.
- Keep Xrm/C# as the primary compatibility surface and Web API as the secondary surface.
- Prove the emulator can be useful under Aspire before broadening into general external-tool compatibility.

## Implemented Behavior

### Local Bootstrap

- Supported local connection string pattern:
  - `AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local`
- Hosted organization service rooted at `/org`.
- CRM-compatible WSDL/XSD metadata exposed from:
  - `/org/XRMServices/2011/Organization.svc?wsdl&sdkversion=9.2`

### Xrm/C# Compatibility

- The real legacy `CrmServiceClient` can bootstrap against the emulator.
- Supported operations:
  - `Create(Entity)`
  - `Retrieve(string, Guid, ColumnSet)`
  - `Update(Entity)`
  - `Delete(string, Guid)`
  - `RetrieveMultiple(QueryExpression)`
- Supported startup and execute message coverage:
  - `RetrieveCurrentOrganization`
  - `WhoAmI`
  - execute-wrapped CRUD/query requests used by the current client path

### QueryExpression Support

- One table: `account`
- `ColumnSet(true)` or explicit columns
- root `AND` filters
- `ConditionOperator.Equal`
- `OrderExpression`
- `TopCount`

### Web API Compatibility

- Service root under `/api/data/v9.2`
- `$metadata`
- CRUD for `/accounts`
- shared metadata and record behavior with the Xrm path

The Web API slice is intentionally narrow and stays aligned with the same local emulator use case as the Xrm path.

### Shared Error Mapping

- Shared application/domain failures map to:
  - SDK-style faults for Xrm/C#
  - Dataverse-style JSON HTTP errors for Web API

## Current Constraints

- One table only: `account`
- In-memory state only
- No broad metadata SDK surface
- No FetchXML
- No relationship traversal
- No auth fidelity beyond permissive local bootstrap
- No promise of Power BI, Power Automate, or broader connector compatibility in this slice

## Out Of Scope

- Full Dataverse parity
- multi-table behavior
- plugin pipeline behavior
- batch and advanced Web API semantics
- connector-specific behavior beyond the local .NET/Xrm scenario

## Verification

This slice is currently proven by:

- Aspire-hosted end-to-end tests
- a reusable `net48` harness that uses the real `CrmServiceClient`
- cross-surface tests that verify Xrm-to-Web API and Web API-to-Xrm flows
