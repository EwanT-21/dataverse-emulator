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
        var contactId = ColumnDefinition.Create(
            "contactid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var contactFullName = ColumnDefinition.Create(
            "fullname",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var contactEmail = ColumnDefinition.Create(
            "emailaddress1",
            AttributeType.String,
            RequiredLevel.None);
        var contactParentCustomer = ColumnDefinition.Create(
            "parentcustomerid",
            AttributeType.Lookup,
            RequiredLevel.None,
            lookupTargetTable: "account",
            lookupRelationshipName: "contact_customer_accounts");
        var contactCreatedOn = ColumnDefinition.Create(
            "createdon",
            AttributeType.DateTime,
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
        var contactTable = TableDefinition.Create(
            logicalName: "contact",
            entitySetName: "contacts",
            primaryIdAttribute: "contactid",
            primaryNameAttribute: "fullname",
            columns:
            [
                contactId.Value,
                contactFullName.Value,
                contactEmail.Value,
                contactParentCustomer.Value,
                contactCreatedOn.Value
            ]);

        Validate(
            accountId,
            accountName,
            accountNumber,
            createdOn,
            active,
            contactId,
            contactFullName,
            contactEmail,
            contactParentCustomer,
            contactCreatedOn,
            accountTable,
            contactTable);

        return new SeedScenario(
            Tables: [accountTable.Value, contactTable.Value],
            Records: Array.Empty<EntityRecord>());
    }

    private static void Validate(
        ErrorOr<ColumnDefinition> accountId,
        ErrorOr<ColumnDefinition> accountName,
        ErrorOr<ColumnDefinition> accountNumber,
        ErrorOr<ColumnDefinition> createdOn,
        ErrorOr<ColumnDefinition> active,
        ErrorOr<ColumnDefinition> contactId,
        ErrorOr<ColumnDefinition> contactFullName,
        ErrorOr<ColumnDefinition> contactEmail,
        ErrorOr<ColumnDefinition> contactParentCustomer,
        ErrorOr<ColumnDefinition> contactCreatedOn,
        ErrorOr<TableDefinition> accountTable,
        ErrorOr<TableDefinition> contactTable)
    {
        if (accountId.IsError
            || accountName.IsError
            || accountNumber.IsError
            || createdOn.IsError
            || active.IsError
            || contactId.IsError
            || contactFullName.IsError
            || contactEmail.IsError
            || contactParentCustomer.IsError
            || contactCreatedOn.IsError
            || accountTable.IsError
            || contactTable.IsError)
        {
            throw new InvalidOperationException("Default emulator seed scenario could not be created.");
        }
    }
}
