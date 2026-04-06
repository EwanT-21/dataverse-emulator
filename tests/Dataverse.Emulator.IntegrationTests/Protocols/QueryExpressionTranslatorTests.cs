using Dataverse.Emulator.Protocols.Xrm;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class QueryExpressionTranslatorTests
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
}
