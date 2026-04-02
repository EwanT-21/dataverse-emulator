# Roadmap

## Phase 0: Foundation

- Establish clean project boundaries.
- Create a runnable host.
- Document architectural intent and phased scope.

## Phase 1: Core Metadata And Records

Implemented or underway:

- Define table and attribute metadata.
- Support seeded entities and deterministic reset behavior.
- Build in-memory storage abstractions for aggregate persistence and record querying.
- Implement a narrow CRUD application pipeline with Mediator and validation behaviors.

Still open in this phase:

- Expand metadata and relationship modeling beyond the current table/row slice.
- Add more compatibility-oriented behaviors on top of the internal CRUD/query workflow.

## Phase 2: First Compatible HTTP Slice

- Add a minimal Dataverse Web API surface.
- Support the query features needed for a first real client path.
- Validate compatibility with one target application or connector.

## Phase 3: Query And Message Expansion

- Add paging, filtering, ordering, and relationship traversal.
- Introduce FetchXML and QueryExpression translation where needed.
- Expand execute-message coverage based on proven demand.

## Phase 4: XRM And Ecosystem Features

- Improve XRM client behavior and message compatibility.
- Add snapshot import/export and environment seeding workflows.
- Explore plugin pipeline hooks, auth modes, and durable storage providers.

## Contribution Rule Of Thumb

Every roadmap item should answer one of these questions clearly:

- Does it improve compatibility with a real Dataverse client path?
- Does it improve deterministic local development workflows?
- Does it make the emulator easier for contributors to understand and extend?
