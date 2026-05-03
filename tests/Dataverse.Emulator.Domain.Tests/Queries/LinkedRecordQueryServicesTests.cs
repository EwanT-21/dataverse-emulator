using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Domain.Tests;

public sealed class LinkedRecordQueryServicesTests
{
    [Fact]
    public void Validate_ReturnsErrors_ForUnknownScopedColumns()
    {
        var accountTable = CreateAccountTable();
        var contactTable = CreateContactTable();

        var query = new LinkedRecordQuery(
            RootTableLogicalName: "contact",
            RootSelectedColumns: ["fullname"],
            Joins:
            [
                new LinkedRecordJoin(
                    TableLogicalName: "account",
                    Alias: "parent",
                    FromAttributeName: "parentcustomerid",
                    ToAttributeName: "accountid",
                    SelectedColumns: ["name"],
                    ReturnAllColumns: false,
                    Filter: null)
            ],
            Filter: new LinkedRecordFilter(
                FilterOperator.And,
                [
                    new LinkedRecordCondition(
                        ScopeName: "parent",
                        ColumnLogicalName: "doesnotexist",
                        Operator: ConditionOperator.Equal,
                        Values: ["Contoso"])
                ],
                []),
            Sorts: [],
            Top: null,
            Page: null,
            CurrentPageNumber: 1);

        var validator = new LinkedRecordQueryValidationService();

        var errors = validator.Validate(
            contactTable,
            new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["parent"] = accountTable
            },
            query);

        Assert.Contains(errors, error => error.Description.Contains("doesnotexist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Execute_Joins_And_Projects_LinkedRows()
    {
        var accountTable = CreateAccountTable();
        var contactTable = CreateContactTable();

        var accountId = Guid.NewGuid();
        var matchingAccount = CreateRecord(
            "account",
            accountId,
            new Dictionary<string, object?>
            {
                ["accountid"] = accountId,
                ["name"] = "Contoso",
                ["accountnumber"] = "A-100"
            });
        var nonMatchingAccount = CreateRecord(
            "account",
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["accountid"] = Guid.NewGuid(),
                ["name"] = "Fabrikam",
                ["accountnumber"] = "F-100"
            });

        var matchingContact = CreateRecord(
            "contact",
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["contactid"] = Guid.NewGuid(),
                ["fullname"] = "Ada Lovelace",
                ["parentcustomerid"] = accountId
            });
        var nonMatchingContact = CreateRecord(
            "contact",
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["contactid"] = Guid.NewGuid(),
                ["fullname"] = "Grace Hopper",
                ["parentcustomerid"] = nonMatchingAccount.Id
            });

        var query = new LinkedRecordQuery(
            RootTableLogicalName: "contact",
            RootSelectedColumns: ["fullname"],
            Joins:
            [
                new LinkedRecordJoin(
                    TableLogicalName: "account",
                    Alias: "parent",
                    FromAttributeName: "parentcustomerid",
                    ToAttributeName: "accountid",
                    SelectedColumns: ["name"],
                    ReturnAllColumns: false,
                    Filter: null)
            ],
            Filter: new LinkedRecordFilter(
                FilterOperator.And,
                [
                    new LinkedRecordCondition(
                        ScopeName: "parent",
                        ColumnLogicalName: "name",
                        Operator: ConditionOperator.Equal,
                        Values: ["Contoso"])
                ],
                []),
            Sorts:
            [
                new LinkedRecordSort(
                    ScopeName: "contact",
                    ColumnLogicalName: "fullname",
                    Direction: SortDirection.Ascending)
            ],
            Top: null,
            Page: null,
            CurrentPageNumber: 1);

        var executor = new LinkedRecordQueryExecutionService();

        var result = executor.Execute(
            query,
            contactTable,
            new Dictionary<string, IReadOnlyList<EntityRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["contact"] = [matchingContact, nonMatchingContact],
                ["account"] = [matchingAccount, nonMatchingAccount]
            });

        Assert.Null(result.ContinuationToken);
        Assert.Single(result.Items);
        Assert.Equal("Ada Lovelace", result.Items[0].RootRecord.Values["fullname"]);
        Assert.False(result.Items[0].RootRecord.Values.Contains("parentcustomerid"));
        Assert.Equal("Contoso", result.Items[0].LinkedRecords["parent"].Values["name"]);
    }

    [Fact]
    public void Execute_LeftOuter_Joins_Preserve_Root_Rows_Without_A_Match()
    {
        var accountTable = CreateAccountTable();
        var contactTable = CreateContactTable();

        var accountId = Guid.NewGuid();
        var matchingAccount = CreateRecord(
            "account",
            accountId,
            new Dictionary<string, object?>
            {
                ["accountid"] = accountId,
                ["name"] = "Contoso"
            });

        var linkedContact = CreateRecord(
            "contact",
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["contactid"] = Guid.NewGuid(),
                ["fullname"] = "Ada Lovelace",
                ["parentcustomerid"] = accountId
            });
        var orphanedContact = CreateRecord(
            "contact",
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["contactid"] = Guid.NewGuid(),
                ["fullname"] = "Grace Hopper"
            });

        var query = new LinkedRecordQuery(
            RootTableLogicalName: "contact",
            RootSelectedColumns: ["fullname"],
            Joins:
            [
                new LinkedRecordJoin(
                    TableLogicalName: "account",
                    Alias: "parent",
                    FromAttributeName: "parentcustomerid",
                    ToAttributeName: "accountid",
                    SelectedColumns: ["name"],
                    ReturnAllColumns: false,
                    Filter: null,
                    JoinType: LinkedRecordJoinType.LeftOuter)
            ],
            Filter: null,
            Sorts:
            [
                new LinkedRecordSort(
                    ScopeName: "contact",
                    ColumnLogicalName: "fullname",
                    Direction: SortDirection.Ascending)
            ],
            Top: null,
            Page: null,
            CurrentPageNumber: 1);

        var executor = new LinkedRecordQueryExecutionService();

        var result = executor.Execute(
            query,
            contactTable,
            new Dictionary<string, IReadOnlyList<EntityRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["contact"] = [linkedContact, orphanedContact],
                ["account"] = [matchingAccount]
            });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Ada Lovelace", result.Items[0].RootRecord.Values["fullname"]);
        Assert.Equal("Contoso", result.Items[0].LinkedRecords["parent"].Values["name"]);
        Assert.Equal("Grace Hopper", result.Items[1].RootRecord.Values["fullname"]);
        Assert.Empty(result.Items[1].LinkedRecords);
    }

    private static TableDefinition CreateAccountTable()
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

        var table = TableDefinition.Create(
            "account",
            "accounts",
            "accountid",
            "name",
            [accountId.Value, name.Value, accountNumber.Value]);

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
        var fullname = ColumnDefinition.Create(
            "fullname",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var parentCustomerId = ColumnDefinition.Create(
            "parentcustomerid",
            AttributeType.Lookup,
            RequiredLevel.None,
            lookupTargetTable: "account");

        var table = TableDefinition.Create(
            "contact",
            "contacts",
            "contactid",
            "fullname",
            [contactId.Value, fullname.Value, parentCustomerId.Value]);

        Assert.False(table.IsError);
        return table.Value;
    }

    private static EntityRecord CreateRecord(
        string tableLogicalName,
        Guid id,
        IReadOnlyDictionary<string, object?> values)
    {
        var recordValues = RecordValues.Create(values);
        Assert.False(recordValues.IsError);

        var record = EntityRecord.Create(tableLogicalName, id, recordValues.Value);
        Assert.False(record.IsError);
        return record.Value;
    }
}
