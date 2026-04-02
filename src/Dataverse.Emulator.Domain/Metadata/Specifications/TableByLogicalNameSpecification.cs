using Ardalis.Specification;

namespace Dataverse.Emulator.Domain.Metadata.Specifications;

public sealed class TableByLogicalNameSpecification : SingleResultSpecification<TableDefinition>
{
    public TableByLogicalNameSpecification(string logicalName)
    {
        Query.Where(table => table.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));
    }
}
