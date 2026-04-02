using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Domain.Tests;

public class TableDefinitionTests
{
    [Fact]
    public void FindColumn_IsCaseInsensitive()
    {
        var table = new TableDefinition(
                logicalName: "account",
                entitySetName: "accounts",
                primaryIdAttribute: "accountid",
                primaryNameAttribute: "name",
                columns:
                [
                    new ColumnDefinition("accountid", AttributeType.UniqueIdentifier, RequiredLevel.None, IsPrimaryId: true),
                    new ColumnDefinition("name", AttributeType.String, RequiredLevel.ApplicationRequired, IsPrimaryName: true)
                ]);

        var column = table.FindColumn("NAME");

        Assert.NotNull(column);
        Assert.Equal("name", column!.LogicalName);
    }
}
