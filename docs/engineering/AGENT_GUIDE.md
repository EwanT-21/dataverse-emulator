# Agent Guide

This repository uses hierarchical `AGENTS.md` files (provider-agnostic — read by Claude Code, Codex CLI, Cursor, Amp, Aider). Each file is auto-loaded when an agent operates from that directory or below. Root `AGENTS.md` is the routing contract; this guide explains how to apply it. **Operate from the narrowest directory that contains your work** — this minimises tokens loaded and reduces wrong-layer mistakes.

## File index

| Path | Scope | One-line purpose |
|---|---|---|
| `/AGENTS.md` | Repo-wide | Routing contract, build/test/run commands, coding style, commit conventions |
| `/CLAUDE.md` | Repo-wide (Claude Code) | Architecture overview, dependency direction, layer ownership table |
| `/src/Dataverse.Emulator.Domain/AGENTS.md` | Domain layer | Sealed records + `Create()` factories, smart enums, transport-agnostic query model |
| `/src/Dataverse.Emulator.Application/AGENTS.md` | Application layer | Mediator handlers, FluentValidation, repository abstractions, naming conventions |
| `/src/Dataverse.Emulator.Protocols/AGENTS.md` | Protocols layer | Xrm/SOAP + Web API translation, narrow request handlers, error mapping |
| `/src/Dataverse.Emulator.Persistence.InMemory/AGENTS.md` | Persistence | In-memory repository, query execution, future-provider boundary |
| `/src/Dataverse.Emulator.Host/AGENTS.md` | Host | Composition root, admin endpoints (`/_emulator/v1/...`), startup seeding |
| `/src/Dataverse.Emulator.AppHost/AGENTS.md` | Aspire orchestration | Public API surface (`AddDataverseEmulator()`, `With*` helpers), connection-string shaping |
| `/tests/AGENTS.md` | Tests | Four-project test layering and decision rules |

## Decision tree — where do I work?

| Task | Operate from | Reason |
|---|---|---|
| Add a new Xrm SDK message handler | `src/Dataverse.Emulator.Protocols/` | Per-message handler slice + translator |
| Extend FetchXML or QueryExpression support | `src/Dataverse.Emulator.Protocols/` | Translator change; domain model usually already supports it |
| Add a new query operator / filter / sort rule | `src/Dataverse.Emulator.Domain/` | Smart enums + execution services live here |
| Add a new CRUD command or list query | `src/Dataverse.Emulator.Application/` | Mediator command + handler + validator slice |
| Add a new admin endpoint | `src/Dataverse.Emulator.Host/` | But the behavior usually lives in `Application/Seeding/` |
| Expose a new Aspire helper | `src/Dataverse.Emulator.AppHost/` | Consumer-facing public API |
| Change in-memory storage shape | `src/Dataverse.Emulator.Persistence.InMemory/` | Repository implementation |
| Add tests for a new slice | `tests/` (and read decision rules in `tests/AGENTS.md`) | Picks the right project per scope |

## Delegation rules

- Delegate by ownership boundary when work spans multiple layers or tests. Keep single-layer fixes local unless the task is large enough to benefit from parallel work.
- Scope each subagent to one layer or one test slice. The parent agent owns decomposition, integration, and final verification.
- Prefer one focused subagent per boundary over many overlapping subagents. Avoid file-per-agent splits.
- Do not work from repo root when a narrower directory covers the task.

## Role taxonomy

Roles describe **how** an agent should think; directories describe **what it owns**. Keep the role set small and stable, then pair each role with the narrowest working directory that fits the task.

| Role | Mode | Typical scope |
|---|---|---|
| `backend-implementer` | Writes or refactors backend code inside one ownership area | One of `src/Dataverse.Emulator.*` |
| `test-writer` | Adds or updates proving tests without owning production edits | `tests/` or one test project |
| `domain-modelling-reviewer` | Read-only review of invariants, domain ownership, and semantic drift | `src/Dataverse.Emulator.Domain/` or a targeted diff |
| `compatibility-reviewer` | Read-only review of Xrm/SOAP, Web API, and real-client compatibility risk | `src/Dataverse.Emulator.Protocols/`, `tests/`, or repo root for a diff |
| `security-reviewer` | Read-only review of secrets, admin surfaces, validation boundaries, and risky config changes | Affected scope or repo root |

### Role + scope examples

- `backend-implementer` in `src/Dataverse.Emulator.Protocols/` for a new Xrm message handler.
- `backend-implementer` in `src/Dataverse.Emulator.Application/` for a new mediator command slice.
- `test-writer` in `tests/Dataverse.Emulator.IntegrationTests/` for protocol translation coverage.
- `domain-modelling-reviewer` against a `Domain` diff before merging query or invariant changes.
- `compatibility-reviewer` after changes that affect request translation, error mapping, or client-visible behavior.

## Delegation matrix

| Task shape | Recommended split |
|---|---|
| New Xrm message plus integration tests | `src/Dataverse.Emulator.Protocols/` implementation agent + `tests/Dataverse.Emulator.IntegrationTests/` test agent |
| New domain query rule plus domain tests | `src/Dataverse.Emulator.Domain/` implementation agent + `tests/Dataverse.Emulator.Domain.Tests/` test agent |
| New application command plus protocol entry point | `src/Dataverse.Emulator.Application/` agent + `src/Dataverse.Emulator.Protocols/` agent + matching `tests/` agent |
| New host endpoint backed by seeding or orchestration logic | `src/Dataverse.Emulator.Host/` agent + `src/Dataverse.Emulator.Application/` agent + matching `tests/` agent |
| Repo-wide documentation or instruction updates | Keep local to the parent agent unless the change is broad enough to justify a separate docs-only agent |

## When not to delegate

- Single-layer, single-file, or obviously bounded edits where coordination costs more than it saves.
- Read-only exploration that needs a coherent view before any decomposition is sensible.
- Small follow-up fixes inside the same scoped directory after one agent has already established the right context.
- Tasks where splitting would force multiple agents to touch the same file set.

## Cross-cutting ownership

| Concern | Owned by |
|---|---|
| Query semantics (filtering, sorting, joining, paging) | `Domain/Services/` + `Domain/Queries/` |
| SDK type ↔ domain query model translation | `Protocols/Xrm/*Translator.cs`, `Protocols/Xrm/Queries/` |
| Validation of request shape at boundary | `Application/*Validator.cs` |
| Domain invariants | `Domain/` factories and aggregates |
| Error mapping (ErrorOr → SDK fault / HTTP error) | `Protocols/Common/DataverseProtocolErrorMapper.cs`, `Protocols/Xrm/DataverseXrmErrors.cs` |
| Seeded state and snapshot import/export | `Application/Seeding/` |
| Xrm request tracing | `Protocols/Xrm/Tracing/` |
| Aspire resource shape and connection-string binding | `AppHost/DataverseEmulatorAppHostExtensions.cs` |

## Example agent invocations

When spawning subagents, set the agent's working directory to the narrowest scope:

```text
# Adding a new Xrm message handler (e.g. SetStateRequest)
cd src/Dataverse.Emulator.Protocols
# Agent loads root + Protocols AGENTS.md only — not Domain/Application/etc.

# Extending a domain rule (e.g. new ConditionOperator)
cd src/Dataverse.Emulator.Domain
# Agent sees domain factory/smart-enum patterns immediately

# Writing tests for a new translator path
cd tests/Dataverse.Emulator.IntegrationTests
# Agent sees test layering rules + existing fixtures
```

## Token discipline

- Start from the narrowest directory that contains the intended work instead of repo root whenever possible.
- Load only the local `AGENTS.md` and directly relevant code or tests before expanding scope.
- Prefer one scoped implementation agent plus one scoped test agent over broad agents that read unrelated layers.
- Expand to a second layer only when the first scoped pass proves the work cannot be completed correctly in place.

## Provider overlays

- Keep routing, scoping, and ownership rules in `AGENTS.md` plus this guide so every provider sees the same contract.
- Provider-specific subagent files should mirror the role taxonomy above, not redefine architecture or ownership.
- For Claude, project-level subagents live in `.claude/agents/`. Treat them as an optional acceleration layer on top of the shared rules.

## Example shell commands (scoped)

```bash
# Build everything
dotnet build Dataverse.Emulator.slnx

# Test a single project (fast iteration)
dotnet test tests/Dataverse.Emulator.Domain.Tests
dotnet test tests/Dataverse.Emulator.IntegrationTests
dotnet test tests/Dataverse.Emulator.AspireTests

# Filter to a single test class
dotnet test tests/Dataverse.Emulator.IntegrationTests --filter "FetchXmlCompatibilityTddTests"

# Run a single test method
dotnet test tests/Dataverse.Emulator.IntegrationTests --filter "FullyQualifiedName~FetchXml_LinkEntity_Filter_Narrows"

# Format
dotnet format Dataverse.Emulator.slnx

# Run the emulator (Aspire-hosted)
dotnet run --project src/Dataverse.Emulator.AppHost

# Run the bare web host
dotnet run --project src/Dataverse.Emulator.Host
```

## File format note

`AGENTS.md` is the open standard read by Codex CLI, Cursor, Amp, Aider, and Claude Code. The repo additionally keeps `CLAUDE.md` at root for Claude-specific architectural framing. Subdirectory files use `AGENTS.md` only — Claude Code reads them, and they remain portable to other agents. This guide is documentation, not executable routing config, so root instructions must point agents here when routing decisions matter.

## Maintenance

- When adding a new ADR or changing a layer's rules, update the relevant project's `AGENTS.md` (not this guide).
- When adding a new project or test project, extend this guide's index and decision tree.
- When a rule starts being violated repeatedly, surface it as a drift signal in the relevant `AGENTS.md` rather than in commit messages.
