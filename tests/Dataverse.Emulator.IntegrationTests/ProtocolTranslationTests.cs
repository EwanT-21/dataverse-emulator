using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using Dataverse.Emulator.Protocols.Xrm;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class ProtocolTranslationTests
{
    [Fact]
    public void QueryExpression_Translates_To_Shared_RecordQuery()
    {
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name", "accountnumber"),
            TopCount = 5
        };
        query.Criteria.AddCondition("name", ConditionOperator.Equal, "Contoso");
        query.Orders.Add(new OrderExpression("name", OrderType.Descending));

        var result = DataverseXrmQueryExpressionTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.Equal("account", result.Value.TableLogicalName);
        Assert.Equal(["name", "accountnumber"], result.Value.SelectedColumns);
        Assert.Single(result.Value.Conditions);
        Assert.Equal("name", result.Value.Conditions[0].ColumnLogicalName);
        Assert.Equal("Contoso", result.Value.Conditions[0].Value);
        Assert.Single(result.Value.Sorts);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.SortDirection.Descending, result.Value.Sorts[0].Direction);
        Assert.Equal(5, result.Value.Top);
    }

    [Fact]
    public void SingleTable_QueryExpression_Translator_Rejects_LinkEntities()
    {
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        query.LinkEntities.Add(new LinkEntity("account", "account", "accountid", "accountid", JoinOperator.Inner));

        var result = DataverseXrmQueryExpressionTranslator.Translate(query);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Protocol.Xrm.Query.Unsupported");
        Assert.Contains(result.Errors, error => error.Description.Contains("LinkEntity", StringComparison.Ordinal));
    }

    [Fact]
    public void QueryExpression_PageInfo_Translates_To_Shared_PageRequest()
    {
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name"),
            PageInfo = new PagingInfo
            {
                Count = 2,
                PageNumber = 3
            }
        };

        var result = DataverseXrmQueryExpressionTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Page);
        Assert.Equal(2, result.Value.Page!.Size);
        Assert.Equal("4", result.Value.Page.ContinuationToken);
    }

    [Fact]
    public void QueryExpression_Nested_Filters_Translate_To_FilterTree()
    {
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name", "accountnumber")
        };
        query.Criteria.AddCondition("accountnumber", ConditionOperator.NotNull);
        var group = query.Criteria.AddFilter(LogicalOperator.Or);
        group.AddCondition("name", ConditionOperator.BeginsWith, "Al");
        group.AddCondition("name", ConditionOperator.Equal, "Charlie");

        var result = DataverseXrmQueryExpressionTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Filter);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.FilterOperator.And, result.Value.Filter!.Operator);
        Assert.Single(result.Value.Filter.Conditions);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.ConditionOperator.NotNull, result.Value.Filter.Conditions[0].Operator);
        Assert.Single(result.Value.Filter.Filters);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.FilterOperator.Or, result.Value.Filter.Filters[0].Operator);
        Assert.Equal(2, result.Value.Filter.Filters[0].Conditions.Count);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.ConditionOperator.BeginsWith, result.Value.Filter.Filters[0].Conditions[0].Operator);
    }

    [Fact]
    public void QueryExpression_In_Condition_Preserves_Multiple_Values()
    {
        var query = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("accountnumber")
        };
        query.Criteria.AddCondition("accountnumber", ConditionOperator.In, "A-100", "C-100");

        var result = DataverseXrmQueryExpressionTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value.Filter);
        Assert.Single(result.Value.Filter!.Conditions);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.ConditionOperator.In, result.Value.Filter.Conditions[0].Operator);
        Assert.Equal(["A-100", "C-100"], result.Value.Filter.Conditions[0].Values.Cast<string>().ToArray());
    }

    [Fact]
    public void Linked_QueryExpression_Translates_To_Application_LinkedRecordQuery()
    {
        var query = new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("fullname")
        };

        query.Criteria.AddCondition("fullname", ConditionOperator.NotNull);
        query.Criteria.AddCondition(new ConditionExpression("name", ConditionOperator.Equal, "Contoso")
        {
            EntityName = "parent"
        });
        query.Orders.Add(new OrderExpression("name", OrderType.Ascending)
        {
            EntityName = "parent"
        });

        var link = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner);
        link.EntityAlias = "parent";
        link.Columns = new ColumnSet("name");
        query.LinkEntities.Add(link);

        var result = DataverseXrmLinkedQueryTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.Equal("contact", result.Value.RootTableLogicalName);
        Assert.Equal(["fullname"], result.Value.RootSelectedColumns);
        Assert.Single(result.Value.Joins);
        Assert.Equal("account", result.Value.Joins[0].TableLogicalName);
        Assert.Equal("parent", result.Value.Joins[0].Alias);
        Assert.Equal("parentcustomerid", result.Value.Joins[0].FromAttributeName);
        Assert.Equal("accountid", result.Value.Joins[0].ToAttributeName);
        Assert.Equal(["name"], result.Value.Joins[0].SelectedColumns);
        Assert.NotNull(result.Value.Filter);
        Assert.Equal(["contact", "parent"], result.Value.Filter!.Conditions.Select(condition => condition.ScopeName).ToArray());
        Assert.Single(result.Value.Sorts);
        Assert.Equal("parent", result.Value.Sorts[0].ScopeName);
        Assert.Equal("name", result.Value.Sorts[0].ColumnLogicalName);
    }

    [Fact]
    public void FetchExpression_Translates_To_Shared_RecordQuery()
    {
        var table = CreateAccountTable();
        var query = new FetchExpression(
            "<fetch count='2' page='2'>" +
            "<entity name='account'>" +
            "<attribute name='name' />" +
            "<attribute name='accountnumber' />" +
            "<filter type='or'>" +
            "<condition attribute='name' operator='begins-with' value='Al' />" +
            "<condition attribute='name' operator='eq' value='Charlie' />" +
            "</filter>" +
            "<order attribute='name' descending='true' />" +
            "</entity>" +
            "</fetch>");

        var result = DataverseXrmFetchExpressionTranslator.Translate(query, table);

        Assert.False(result.IsError);
        Assert.Equal("account", result.Value.Query.TableLogicalName);
        Assert.Equal(["name", "accountnumber"], result.Value.Query.SelectedColumns);
        Assert.NotNull(result.Value.Query.Filter);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.FilterOperator.Or, result.Value.Query.Filter!.Operator);
        Assert.Equal(2, result.Value.Query.Filter.Conditions.Count);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.ConditionOperator.BeginsWith, result.Value.Query.Filter.Conditions[0].Operator);
        Assert.Single(result.Value.Query.Sorts);
        Assert.Equal(Dataverse.Emulator.Domain.Queries.SortDirection.Descending, result.Value.Query.Sorts[0].Direction);
        Assert.NotNull(result.Value.Query.Page);
        Assert.Equal(2, result.Value.Query.Page!.Size);
        Assert.Equal("2", result.Value.Query.Page.ContinuationToken);
        Assert.Equal(2, result.Value.CurrentPageNumber);
    }

    [Fact]
    public void FetchExpression_Rejects_LinkEntity()
    {
        var table = CreateAccountTable();
        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='account'>" +
            "<attribute name='name' />" +
            "<link-entity name='account' from='accountid' to='accountid' alias='child' />" +
            "</entity>" +
            "</fetch>");

        var result = DataverseXrmFetchExpressionTranslator.Translate(query, table);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Protocol.Xrm.FetchXml.Unsupported");
        Assert.Contains(result.Errors, error => error.Description.Contains("link-entity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Shared_Error_Model_Maps_To_Sdk_Faults()
    {
        var fault = DataverseProtocolErrorMapper.ToFaultException(
            [Error.Validation("Protocol.Test", "A validation error occurred.")]);

        Assert.Equal(unchecked((int)0x80040203), fault.Detail.ErrorCode);
        Assert.Equal("A validation error occurred.", fault.Detail.Message);
        Assert.Equal("Protocol.Test", fault.Detail.ErrorDetails["DataverseEmulator.ErrorCode"]);
    }

    [Fact]
    public void Entity_Metadata_Maps_Primary_Information_And_Attributes()
    {
        var table = CreateAccountTable();
        var metadata = DataverseXrmMetadataMapper.ToEntityMetadata(table, EntityFilters.Entity | EntityFilters.Attributes);

        Assert.Equal("account", metadata.LogicalName);
        Assert.Equal("accounts", metadata.EntitySetName);
        Assert.Equal("accountid", metadata.PrimaryIdAttribute);
        Assert.Equal("name", metadata.PrimaryNameAttribute);
        Assert.Equal(4, metadata.Attributes.Length);
        Assert.Contains(metadata.Attributes, attribute => attribute.LogicalName == "name" && attribute.IsPrimaryName == true);
    }

    private static Dataverse.Emulator.Domain.Metadata.TableDefinition CreateAccountTable()
    {
        var accountId = Dataverse.Emulator.Domain.Metadata.ColumnDefinition.Create(
            "accountid",
            Dataverse.Emulator.Domain.Metadata.AttributeType.UniqueIdentifier,
            Dataverse.Emulator.Domain.Metadata.RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var name = Dataverse.Emulator.Domain.Metadata.ColumnDefinition.Create(
            "name",
            Dataverse.Emulator.Domain.Metadata.AttributeType.String,
            Dataverse.Emulator.Domain.Metadata.RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var accountNumber = Dataverse.Emulator.Domain.Metadata.ColumnDefinition.Create(
            "accountnumber",
            Dataverse.Emulator.Domain.Metadata.AttributeType.String,
            Dataverse.Emulator.Domain.Metadata.RequiredLevel.None);
        var active = Dataverse.Emulator.Domain.Metadata.ColumnDefinition.Create(
            "isactive",
            Dataverse.Emulator.Domain.Metadata.AttributeType.Boolean,
            Dataverse.Emulator.Domain.Metadata.RequiredLevel.None);
        var table = Dataverse.Emulator.Domain.Metadata.TableDefinition.Create(
            "account",
            "accounts",
            "accountid",
            "name",
            [accountId.Value, name.Value, accountNumber.Value, active.Value]);

        Assert.False(table.IsError);
        return table.Value;
    }
}
