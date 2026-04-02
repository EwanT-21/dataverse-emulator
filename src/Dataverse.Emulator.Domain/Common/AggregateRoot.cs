namespace Dataverse.Emulator.Domain.Common;

public abstract class AggregateRoot
{
    protected AggregateRoot(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }

    internal Guid AggregateId { get; }
}
