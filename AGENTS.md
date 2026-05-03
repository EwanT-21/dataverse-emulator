# Repository Guidelines

## Project Structure & Module Organization

This is a .NET Dataverse emulator solution. Production code lives under `src/`:

- `Dataverse.Emulator.AppHost`: Aspire orchestration entry point.
- `Dataverse.Emulator.Host`: web host, health/admin endpoints, protocol registration, and startup seeding.
- `Dataverse.Emulator.Domain`: core table, metadata, record, and query model.
- `Dataverse.Emulator.Application`: handlers, validation, seeding, and orchestration.
- `Dataverse.Emulator.Protocols`: Xrm/SOAP and Web API adapters, translators, and error mapping.
- `Dataverse.Emulator.Persistence.InMemory`: local in-memory persistence provider.

Tests live under `tests/`, split into domain unit tests, integration tests, Aspire end-to-end tests, and the `net48` `CrmServiceClient` harness. Architecture notes are in `docs/architecture.md`.

## Build, Test, and Development Commands

- `dotnet restore Dataverse.Emulator.slnx`: restore solution dependencies.
- `dotnet build Dataverse.Emulator.slnx`: compile all projects.
- `dotnet test Dataverse.Emulator.slnx`: run xUnit test projects.
- `dotnet run --project src/Dataverse.Emulator.AppHost`: start the default Aspire local environment.
- `dotnet run --project src/Dataverse.Emulator.Host`: run only the emulator web host.
- `dotnet format Dataverse.Emulator.slnx`: apply .NET formatting and code style fixes.

## Agent Workflow

- Root `AGENTS.md` is the provider-neutral routing contract for this repository. `CLAUDE.md` adds Claude-specific architecture context only.
- Before planning or editing any task that is ambiguous or touches multiple ownership areas, read `docs/engineering/AGENT_GUIDE.md`.
- If work spans more than one ownership area (`Domain`, `Application`, `Protocols`, `Persistence.InMemory`, `Host`, `AppHost`, or `tests`), split it by scope instead of working from repo root unless the change is genuinely trivial.
- Run each subagent from the narrowest directory that fully contains its work so it loads only the relevant local `AGENTS.md`.
- Roles are cognitive mode; directories are ownership scope. Pair a small stable role set with the existing coarse directory boundaries instead of adding deeper nested `AGENTS.md` files.
- The parent agent owns decomposition, cross-layer decisions, final integration, and final verification. Each subagent owns one layer or one test slice and should avoid editing outside that scope unless reassigned.
- Prefer one focused subagent per ownership boundary rather than one subagent per file. Do not fragment simple single-layer fixes.
- When implementation and tests naturally live in different areas, treat them as separate scopes. A protocol change plus integration tests usually means one scoped implementation agent and one scoped test agent.

## Coding Style & Naming Conventions

Use C# with nullable reference types and implicit usings enabled for `net10.0` projects. Keep indentation at four spaces. Prefer sealed classes where extension is not intended, as used throughout the domain and protocol layers. Name request handlers as `<Operation>XrmRequestHandler`, validators as `<CommandOrQuery>Validator`, and tests as `<Subject>Tests`. Keep transport-specific code in `Protocols`; domain logic should stay transport-agnostic.

## Testing Guidelines

The test suite uses xUnit (`[Fact]` and `[Theory]`). Add focused domain tests for pure behavior, integration tests for application/protocol translation, and Aspire tests for hosted workflows or real client compatibility. Keep test names descriptive and behavior-oriented. Run `dotnet test Dataverse.Emulator.slnx` before submitting changes; use narrower project test commands while iterating.

## Commit & Pull Request Guidelines

Recent commits use short, imperative summaries such as `Add upsert support.` and `Tighten Xrm boundaries.` Follow that style: one concise sentence, capitalized, ending with a period. Pull requests should describe the compatibility or workflow changed, list tests run, and link related issues. Include screenshots or trace samples only when UI, diagnostics, or hosted behavior changes benefit from them.

## Security & Configuration Tips

The emulator is local-first and permissive by design. Do not introduce real secrets into code, tests, snapshots, or examples. Use documented local connection strings and environment variables from `DataverseEmulatorHostEnvironmentVariables` for seed scenarios, snapshots, organization version, and Xrm trace limits.
