using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using Dataverse.Emulator.Protocols.Xrm;
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
    public void QueryExpression_Rejects_LinkEntities()
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
        var active = Dataverse.Emulator.Domain.Metadata.ColumnDefinition.Create(
            "isactive",
            Dataverse.Emulator.Domain.Metadata.AttributeType.Boolean,
            Dataverse.Emulator.Domain.Metadata.RequiredLevel.None);
        var table = Dataverse.Emulator.Domain.Metadata.TableDefinition.Create(
            "account",
            "accounts",
            "accountid",
            "name",
            [accountId.Value, name.Value, active.Value]);

        Assert.False(table.IsError);

        var metadata = DataverseXrmMetadataMapper.ToEntityMetadata(table.Value, EntityFilters.Entity | EntityFilters.Attributes);

        Assert.Equal("account", metadata.LogicalName);
        Assert.Equal("accounts", metadata.EntitySetName);
        Assert.Equal("accountid", metadata.PrimaryIdAttribute);
        Assert.Equal("name", metadata.PrimaryNameAttribute);
        Assert.Equal(3, metadata.Attributes.Length);
        Assert.Contains(metadata.Attributes, attribute => attribute.LogicalName == "name" && attribute.IsPrimaryName == true);
    }
}
