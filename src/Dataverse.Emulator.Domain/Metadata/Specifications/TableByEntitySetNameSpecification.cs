using Ardalis.Specification;

namespace Dataverse.Emulator.Domain.Metadata.Specifications;

public sealed class TableByEntitySetNameSpecification : SingleResultSpecification<TableDefinition>
{
    public TableByEntitySetNameSpecification(string entitySetName)
    {
        Query.Where(table => table.EntitySetName.Equals(entitySetName, StringComparison.OrdinalIgnoreCase));
    }
}
