using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.IntegrationTests;

internal static class ProtocolTestMetadataFactory
{
    public static TableDefinition CreateAccountTable()
    {
        var accountId = ColumnDefinition.Create(
            "accountid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var name = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var accountNumber = ColumnDefinition.Create(
            "accountnumber",
            AttributeType.String,
            RequiredLevel.None);
        var active = ColumnDefinition.Create(
            "isactive",
            AttributeType.Boolean,
            RequiredLevel.None);
        var table = TableDefinition.Create(
            "account",
            "accounts",
            "accountid",
            "name",
            [accountId.Value, name.Value, accountNumber.Value, active.Value]);

        Assert.False(table.IsError);
        return table.Value;
    }

    public static TableDefinition CreateContactTable()
    {
        var contactId = ColumnDefinition.Create(
            "contactid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var fullName = ColumnDefinition.Create(
            "fullname",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var parentCustomerId = ColumnDefinition.Create(
            "parentcustomerid",
            AttributeType.Lookup,
            RequiredLevel.None,
            lookupTargetTable: "account",
            lookupRelationshipName: "contact_customer_accounts");
        var table = TableDefinition.Create(
            "contact",
            "contacts",
            "contactid",
            "fullname",
            [contactId.Value, fullName.Value, parentCustomerId.Value]);

        Assert.False(table.IsError);
        return table.Value;
    }

    public static SeedScenario CreateDefaultXrmScenario(params EntityRecord[] records)
        => new(
            Tables: [CreateAccountTable(), CreateContactTable()],
            Records: records);

    public static EntityRecord CreateAccountRecord(
        Guid id,
        string name,
        string? accountNumber = null,
        bool? isActive = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["accountid"] = id,
            ["name"] = name
        };

        if (accountNumber is not null)
        {
            values["accountnumber"] = accountNumber;
        }

        if (isActive.HasValue)
        {
            values["isactive"] = isActive.Value;
        }

        return CreateRecord("account", id, values);
    }

    public static EntityRecord CreateContactRecord(
        Guid id,
        string fullName,
        Guid? parentCustomerId = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["contactid"] = id,
            ["fullname"] = fullName
        };

        if (parentCustomerId.HasValue)
        {
            values["parentcustomerid"] = parentCustomerId.Value;
        }

        return CreateRecord("contact", id, values);
    }

    private static EntityRecord CreateRecord(
        string logicalName,
        Guid id,
        IReadOnlyDictionary<string, object?> values)
    {
        var recordValues = RecordValues.Create(new Dictionary<string, object?>(values));
        Assert.False(recordValues.IsError);

        var record = EntityRecord.Create(logicalName, id, recordValues.Value);
        Assert.False(record.IsError);
        return record.Value;
    }
}
