# ADR-014: Prefer Demand-Driven Compatibility Over Speculative Platform Parity

- Status: Accepted
- Date: 2026-05-04

## Context

Dataverse exposes a large and uneven compatibility surface:

- Xrm/SOAP messages
- Web API behavior
- metadata breadth
- query semantics
- connector-specific expectations

Treating all of that breadth as equally valuable would push the emulator toward shallow feature accumulation instead of a coherent local-development product.

The project's actual goal is narrower:

- unblock real local application paths
- preserve real bootstrap and compatibility seams
- keep unsupported behavior explicit and maintainable

## Decision

We will prefer demand-driven compatibility over speculative platform parity.

That means:

- new compatibility work should start from a real local workflow, client path, trace, or harness scenario
- each delivery slice should implement only the narrowest metadata, query, and message behavior that path requires
- unsupported breadth should continue to fail explicitly rather than being approximated loosely
- roadmap and spec language should describe bounded slices, not abstract parity goals

## Rationale

- Dataverse parity is too broad to be a useful near-term planning unit.
- Narrow, verified slices are easier to reason about, test, and maintain.
- Real local workflows produce better design pressure than speculative protocol checklists.
- Explicit non-support is often more useful than a partial behavior that looks successful but behaves incorrectly.

## Consequences

- Compatibility progress should be measured by completed local workflows and verified request slices, not by raw feature count.
- Specs should call out both supported breadth and intentional limits.
- Unsupported-path tests are first-class artifacts when they protect project scope and make behavior predictable.
- Broader platform surfaces such as connector-oriented parity remain valid future work only when a concrete workflow justifies them.
