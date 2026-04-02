using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Application.Seeding;

public sealed record SeedScenario(
    IReadOnlyCollection<TableDefinition> Tables,
    IReadOnlyCollection<EntityRecord> Records);
