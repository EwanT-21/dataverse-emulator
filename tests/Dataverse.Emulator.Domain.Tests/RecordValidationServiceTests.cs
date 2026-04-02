using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Domain.Tests;

public class RecordValidationServiceTests
{
    [Fact]
    public void ValidateCreate_ReturnsErrors_ForMissingRequiredAndUnknownColumns()
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

        var validator = new RecordValidationService();
        var values = new Dictionary<string, object?>
        {
            ["notreal"] = "value"
        };

        var errors = validator.ValidateCreate(table, values);

        Assert.Contains(errors, error => error.Description.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Description.Contains("notreal", StringComparison.OrdinalIgnoreCase));
    }
}
