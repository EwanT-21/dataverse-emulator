# ADR-016: Keep Agent Guidance Provider-Neutral And Overlays Thin

- Status: Accepted
- Date: 2026-05-04

## Context

The repository is intended to support ongoing maintenance with a mix of:

- human contributors
- AI-assisted contributors
- more than one agent provider or tool

Provider-specific instruction systems can improve ergonomics, but they create a maintenance risk when core repository guidance is duplicated across multiple files with slightly different wording or behavior.

The project already has durable architectural and contribution constraints that should not vary by provider:

- layer ownership
- routing and scoping rules
- build, test, and run commands
- commit and review expectations

## Decision

We will keep agent guidance provider-neutral by default and treat provider-specific overlays as thin accelerators.

That means:

- root `AGENTS.md` is the primary repository-wide guidance contract
- subdirectory `AGENTS.md` files are the local ownership rules for each major area
- `docs/engineering/AGENT_GUIDE.md` explains routing, scoping, and delegation policy
- provider-specific files such as `CLAUDE.md` or `.claude/agents/*` may add acceleration, but they should mirror the shared guidance rather than redefine architecture or ownership

## Rationale

- Shared rules are easier to maintain than parallel instruction systems.
- A provider-neutral baseline keeps contributor behavior more consistent across tools.
- Thin overlays allow provider-specific ergonomics without turning the repository into a forked instruction set.
- Clear ownership and routing rules reduce token waste and wrong-layer edits even when the underlying provider changes.

## Consequences

- Core workflow and architecture guidance should be updated in `AGENTS.md` and related shared docs first.
- Provider-specific overlays should be reviewed for drift whenever the shared guidance changes materially.
- New agent-oriented documentation should describe repository behavior, not tool branding.
- The repository remains usable when a contributor honors only the provider-neutral guidance layer.
