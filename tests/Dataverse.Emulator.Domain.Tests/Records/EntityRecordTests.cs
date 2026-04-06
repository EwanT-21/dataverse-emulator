using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Domain.Tests;

public class EntityRecordTests
{
    [Fact]
    public void ApplyChanges_PreservesAggregateId()
    {
        var values = RecordValues.Create(
            [
                new KeyValuePair<string, object?>("name", "Contoso")
            ]);
        var record = EntityRecord.Create("account", Guid.NewGuid(), values.Value);

        Assert.False(values.IsError);
        Assert.False(record.IsError);

        var updated = record.Value.ApplyChanges(
            [
                new KeyValuePair<string, object?>("name", "Fabrikam")
            ]);

        Assert.Equal(record.Value.AggregateId, updated.AggregateId);
        Assert.Equal("Fabrikam", updated.Values["name"]);
    }
}
