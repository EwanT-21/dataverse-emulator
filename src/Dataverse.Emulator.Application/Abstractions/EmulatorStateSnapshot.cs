using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Application.Abstractions;

public sealed record EmulatorStateSnapshot(
    IReadOnlyList<TableDefinition> Tables,
    IReadOnlyList<EntityRecord> Records);
