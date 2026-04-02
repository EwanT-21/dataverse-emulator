using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Domain.Tests;

public class TableDefinitionTests
{
    [Fact]
    public void FindColumn_IsCaseInsensitive()
    {
        var table = CreateAccountTable();

        var column = table.FindColumn("NAME");

        Assert.NotNull(column);
        Assert.Equal("name", column!.LogicalName);
    }

    [Fact]
    public void Create_AssignsAggregateId()
    {
        var table = CreateAccountTable();

        Assert.NotEqual(Guid.Empty, table.AggregateId);
    }

    private static TableDefinition CreateAccountTable()
    {
        var idColumn = ColumnDefinition.Create(
            "accountid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.None,
            isPrimaryId: true);
        var nameColumn = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var table = TableDefinition.Create(
            logicalName: "account",
            entitySetName: "accounts",
            primaryIdAttribute: "accountid",
            primaryNameAttribute: "name",
            columns:
            [
                idColumn.Value,
                nameColumn.Value
            ]);

        Assert.False(idColumn.IsError);
        Assert.False(nameColumn.IsError);
        Assert.False(table.IsError);

        return table.Value;
    }
}
