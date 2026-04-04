using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using ErrorOr;

namespace Dataverse.Emulator.Host;

internal static class DefaultSeedScenarioFactory
{
    public static SeedScenario Create()
    {
        var accountId = ColumnDefinition.Create(
            "accountid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var accountName = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var accountNumber = ColumnDefinition.Create(
            "accountnumber",
            AttributeType.String,
            RequiredLevel.None);
        var createdOn = ColumnDefinition.Create(
            "createdon",
            AttributeType.DateTime,
            RequiredLevel.None);
        var active = ColumnDefinition.Create(
            "isactive",
            AttributeType.Boolean,
            RequiredLevel.None);

        var accountTable = TableDefinition.Create(
            logicalName: "account",
            entitySetName: "accounts",
            primaryIdAttribute: "accountid",
            primaryNameAttribute: "name",
            columns:
            [
                accountId.Value,
                accountName.Value,
                accountNumber.Value,
                createdOn.Value,
                active.Value
            ]);

        Validate(accountId, accountName, accountNumber, createdOn, active, accountTable);

        return new SeedScenario(
            Tables: [accountTable.Value],
            Records: Array.Empty<EntityRecord>());
    }

    private static void Validate(
        ErrorOr<ColumnDefinition> accountId,
        ErrorOr<ColumnDefinition> accountName,
        ErrorOr<ColumnDefinition> accountNumber,
        ErrorOr<ColumnDefinition> createdOn,
        ErrorOr<ColumnDefinition> active,
        ErrorOr<TableDefinition> accountTable)
    {
        if (accountId.IsError || accountName.IsError || accountNumber.IsError || createdOn.IsError || active.IsError || accountTable.IsError)
        {
            throw new InvalidOperationException("Default emulator seed scenario could not be created.");
        }
    }
}
