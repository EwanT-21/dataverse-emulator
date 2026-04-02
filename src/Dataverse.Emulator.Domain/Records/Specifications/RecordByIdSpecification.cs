using Ardalis.Specification;

namespace Dataverse.Emulator.Domain.Records.Specifications;

public sealed class RecordByIdSpecification : SingleResultSpecification<EntityRecord>
{
    public RecordByIdSpecification(string tableLogicalName, Guid id)
    {
        Query.Where(
            record => record.TableLogicalName.Equals(tableLogicalName, StringComparison.OrdinalIgnoreCase)
                && record.Id == id);
    }
}
