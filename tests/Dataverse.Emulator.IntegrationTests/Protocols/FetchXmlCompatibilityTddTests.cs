using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class FetchXmlCompatibilityTddTests
{
    [Fact]
    public async Task FetchXml_LinkEntity_Filter_Narrows_Results_To_Matching_Linked_Records()
    {
        var alphaAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bravoAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(alphaAccountId, "Alpha Account"),
                ProtocolTestMetadataFactory.CreateAccountRecord(bravoAccountId, "Bravo Account"),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alice Alpha",
                    alphaAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Bianca Bravo",
                    bravoAccountId)));

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "<filter>" +
            "<condition attribute='name' operator='eq' value='Alpha Account' />" +
            "</filter>" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Entities);
        Assert.Equal("Alice Alpha", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alpha Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
    }

    [Fact]
    public async Task FetchXml_LinkEntity_Order_Sorts_By_Linked_Column()
    {
        var alphaAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bravoAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(alphaAccountId, "Alpha Account"),
                ProtocolTestMetadataFactory.CreateAccountRecord(bravoAccountId, "Bravo Account"),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alice Alpha",
                    alphaAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Bianca Bravo",
                    bravoAccountId)));

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "<order attribute='name' descending='true' />" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Entities.Count);
        Assert.Equal("Bianca Bravo", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Bravo Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
        Assert.Equal("Alice Alpha", result.Value.Entities[1].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alpha Account", (string)((AliasedValue)result.Value.Entities[1]["parent.name"]).Value);
    }

    [Fact]
    public async Task FetchXml_LinkEntity_Filter_And_Order_Compose_Correctly()
    {
        var alphaAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bravoAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var charlieAccountId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(alphaAccountId, "Alpha Account", "A-100"),
                ProtocolTestMetadataFactory.CreateAccountRecord(bravoAccountId, "Bravo Account", "B-200"),
                ProtocolTestMetadataFactory.CreateAccountRecord(charlieAccountId, "Charlie Account", "C-300"),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alice Alpha",
                    alphaAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Bianca Bravo",
                    bravoAccountId),
                ProtocolTestMetadataFactory.CreateContactRecord(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "Charlie Contact",
                    charlieAccountId)));

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "<filter type='or'>" +
            "<condition attribute='accountnumber' operator='eq' value='A-100' />" +
            "<condition attribute='accountnumber' operator='eq' value='B-200' />" +
            "</filter>" +
            "<order attribute='name' descending='false' />" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Entities.Count);
        Assert.Equal("Alice Alpha", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Bianca Bravo", result.Value.Entities[1].GetAttributeValue<string>("fullname"));
    }

    [Fact]
    public async Task FetchXml_Root_Filter_Can_Target_A_Linked_Entity_By_Alias()
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

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<filter>" +
            "<condition entityname='parent' attribute='accountnumber' operator='eq' value='B-200' />" +
            "</filter>" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "<attribute name='accountnumber' />" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Entities);
        Assert.Equal("Bianca Bravo", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Bravo Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
    }

    [Fact]
    public async Task FetchXml_Root_Order_Can_Sort_By_A_Linked_Entity_Column()
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

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<order entityname='parent' attribute='name' descending='true' />" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Entities.Count);
        Assert.Equal("Bianca Bravo", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alice Alpha", result.Value.Entities[1].GetAttributeValue<string>("fullname"));
    }

    [Fact]
    public async Task FetchXml_LinkEntity_Projection_Reuses_The_Shared_Linked_Query_Semantics()
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

        var query = new FetchExpression(
            "<fetch>" +
            "<entity name='contact'>" +
            "<attribute name='fullname' />" +
            "<order attribute='fullname' descending='false' />" +
            "<link-entity name='account' from='accountid' to='parentcustomerid' alias='parent'>" +
            "<attribute name='name' />" +
            "</link-entity>" +
            "</entity>" +
            "</fetch>");

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Entities.Count);
        Assert.Equal("Alice Alpha", result.Value.Entities[0].GetAttributeValue<string>("fullname"));
        Assert.Equal("Alpha Account", (string)((AliasedValue)result.Value.Entities[0]["parent.name"]).Value);
        Assert.Equal("Bianca Bravo", result.Value.Entities[1].GetAttributeValue<string>("fullname"));
        Assert.Equal("Bravo Account", (string)((AliasedValue)result.Value.Entities[1]["parent.name"]).Value);
    }
}
