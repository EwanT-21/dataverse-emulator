using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Domain.Tests;

public class RecordValidationServiceTests
{
    [Fact]
    public void ValidateCreate_ReturnsErrors_ForMissingRequiredAndUnknownColumns()
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

        var validator = new RecordValidationService();
        var values = new Dictionary<string, object?>
        {
            ["notreal"] = "value"
        };

        Assert.False(idColumn.IsError);
        Assert.False(nameColumn.IsError);
        Assert.False(table.IsError);

        var errors = validator.ValidateCreate(table.Value, values);

        Assert.Contains(errors, error => error.Description.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Description.Contains("notreal", StringComparison.OrdinalIgnoreCase));
    }
}
