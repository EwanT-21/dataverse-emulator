using Dataverse.Emulator.Protocols.Xrm.Queries;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class LinkedQueryCompatibilityTddTests
{
    [Fact]
    public async Task LinkCriteria_On_A_Supported_Inner_Join_Are_Applied_On_The_Linked_Scope()
    {
        var alphaAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bravoAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(alphaAccountId, "Alpha Account", "A-100"),
                ProtocolTestMetadataFactory.CreateAccountRecord(bravoAccountId, "Bravo Account", "B-200"),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alice Alpha",
                    alphaAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Bianca Bravo",
                    bravoAccountId)));

        var query = new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("fullname")
        };
        query.Orders.Add(new OrderExpression("fullname", OrderType.Ascending));

        var parentLink = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner)
        {
            EntityAlias = "parent",
            Columns = new ColumnSet("name")
        };
        parentLink.LinkCriteria.AddCondition("name", ConditionOperator.Equal, "Alpha Account");
        query.LinkEntities.Add(parentLink);

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Entities);
        Assert.Equal("Alice Alpha", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alpha Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
    }

    [Fact]
    public async Task LeftOuter_LinkEntity_Preserves_Root_Records_Without_A_Match()
    {
        var alphaAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(alphaAccountId, "Alpha Account", "A-100"),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alice Alpha",
                    alphaAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Orphaned Contact")));

        var query = new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("fullname")
        };
        query.Orders.Add(new OrderExpression("fullname", OrderType.Ascending));

        var parentLink = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.LeftOuter)
        {
            EntityAlias = "parent",
            Columns = new ColumnSet("name")
        };
        query.LinkEntities.Add(parentLink);

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Entities.Count);
        Assert.Equal("Alice Alpha", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alpha Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
        Assert.Equal("Orphaned Contact", result.Value.Entities[1].GetAttributeValue<string>("fullname"));
        Assert.False(result.Value.Entities[1].Attributes.Contains("parent.name"));
    }

    [Fact]
    public void Nested_LinkEntities_Can_Translate_Into_Shared_Linked_Query_Scopes()
    {
        var query = new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("fullname")
        };

        var parentLink = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner)
        {
            EntityAlias = "parent",
            Columns = new ColumnSet("name")
        };

        var siblingLink = new LinkEntity("account", "contact", "accountid", "parentcustomerid", JoinOperator.Inner)
        {
            EntityAlias = "sibling",
            Columns = new ColumnSet("fullname")
        };

        parentLink.LinkEntities.Add(siblingLink);
        query.LinkEntities.Add(parentLink);

        var result = DataverseXrmLinkedQueryTranslator.Translate(query);

        Assert.False(result.IsError);
        Assert.Equal(["parent", "sibling"], result.Value.Joins.Select(join => join.Alias).ToArray());
    }
}
