using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Domain.Tests;

public sealed class LookupRelationshipDefinitionServiceTests
{
    [Fact]
    public void Resolve_Returns_Seeded_Lookup_Relationship_Metadata()
    {
        var accountTable = CreateAccountTable();
        var contactTable = CreateContactTable();
        var service = new LookupRelationshipDefinitionService();

        var result = service.Resolve(
            [accountTable, contactTable],
            "contact_customer_accounts");

        Assert.False(result.IsError);
        Assert.Equal("account", result.Value.ReferencedTableLogicalName);
        Assert.Equal("accountid", result.Value.ReferencedAttributeLogicalName);
        Assert.Equal("contact", result.Value.ReferencingTableLogicalName);
        Assert.Equal("parentcustomerid", result.Value.ReferencingAttributeLogicalName);
    }

    [Fact]
    public void Resolve_Returns_Error_For_Unknown_Relationship()
    {
        var accountTable = CreateAccountTable();
        var contactTable = CreateContactTable();
        var service = new LookupRelationshipDefinitionService();

        var result = service.Resolve(
            [accountTable, contactTable],
            "unknown_relationship");

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Metadata.Relationship.Unknown");
    }

    [Fact]
    public void ColumnDefinition_Rejects_LookupRelationship_Name_On_NonLookup_Columns()
    {
        var result = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            lookupRelationshipName: "contact_customer_accounts");

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Metadata.Column.LookupConfigurationInvalid");
    }

    private static TableDefinition CreateAccountTable()
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
        var table = TableDefinition.Create(
            "account",
            "accounts",
            "accountid",
            "name",
            [accountId.Value, accountName.Value]);

        Assert.False(accountId.IsError);
        Assert.False(accountName.IsError);
        Assert.False(table.IsError);
        return table.Value;
    }

    private static TableDefinition CreateContactTable()
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

        Assert.False(contactId.IsError);
        Assert.False(fullName.IsError);
        Assert.False(parentCustomerId.IsError);
        Assert.False(table.IsError);
        return table.Value;
    }
}
