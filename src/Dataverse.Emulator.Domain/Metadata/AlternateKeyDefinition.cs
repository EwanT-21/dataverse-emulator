using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed record AlternateKeyDefinition
{
    internal AlternateKeyDefinition(
        string logicalName,
        IReadOnlyList<string> columnLogicalNames)
    {
        LogicalName = logicalName;
        ColumnLogicalNames = columnLogicalNames;
    }

    public string LogicalName { get; }

    public IReadOnlyList<string> ColumnLogicalNames { get; }

    public static ErrorOr<AlternateKeyDefinition> Create(
        string logicalName,
        IReadOnlyList<string> columnLogicalNames)
    {
        if (columnLogicalNames is null)
        {
            return DomainErrors.Validation(
                "Metadata.AlternateKey.ColumnsRequired",
                "Alternate key columns are required.");
        }

        if (string.IsNullOrWhiteSpace(logicalName))
        {
            return DomainErrors.Validation(
                "Metadata.AlternateKey.LogicalNameRequired",
                "Alternate key logical name is required.");
        }

        if (columnLogicalNames.Count == 0 || columnLogicalNames.Any(string.IsNullOrWhiteSpace))
        {
            return DomainErrors.Validation(
                "Metadata.AlternateKey.ColumnsInvalid",
                "Alternate key must contain at least one valid column logical name.");
        }

        return new AlternateKeyDefinition(logicalName, columnLogicalNames.ToArray());
    }
}
