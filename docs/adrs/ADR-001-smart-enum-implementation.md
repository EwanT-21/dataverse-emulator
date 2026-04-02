# ADR-001: Use SmartEnum For Domain Enumerations

- Status: Accepted
- Date: 2026-04-02

## Context

The emulator has several concepts that look like enums at first glance but are likely to evolve over time:

- attribute types
- required levels
- condition operators
- sort directions

Plain enums are lightweight, but they make it awkward to attach behavior, aliases, parsing rules, metadata, or transport-specific mappings as the project grows. The project is also intended to be open source, so readability and discoverability matter.

## Decision

We will use `Ardalis.SmartEnum` in place of raw C# enums for domain-level enumerated concepts.

Initial SmartEnum-backed types include:

- `AttributeType`
- `RequiredLevel`
- `ConditionOperator`
- `SortDirection`

These types live in the domain and form part of the emulator's ubiquitous language.

## Rationale

- SmartEnums provide a better developer experience for attaching behavior and parsing logic than raw enums.
- They allow us to grow from simple constants into richer semantic objects without a broad refactor later.
- They are a good fit for Dataverse concepts, where transport mappings and metadata interpretation often need more than an integer constant.

## Consequences

- Equality checks should use SmartEnum instances rather than enum switch expressions.
- Code that previously relied on compile-time enum switch matching will use direct comparisons or type-specific behavior methods instead.
- If a concept stays permanently trivial, SmartEnum may feel slightly heavier than a plain enum, but that trade-off is acceptable for this project.
