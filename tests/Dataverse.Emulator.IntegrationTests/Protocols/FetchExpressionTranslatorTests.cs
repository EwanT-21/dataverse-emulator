using Dataverse.Emulator.Protocols.Xrm;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class FetchExpressionTranslatorTests
{
    [Fact]
    public void FetchExpression_Translates_To_Shared_RecordQuery()
    {
        var table = ProtocolTestMetadataFactory.CreateAccountTable();
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
        var table = ProtocolTestMetadataFactory.CreateAccountTable();
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
}
