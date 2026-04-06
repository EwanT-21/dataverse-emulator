using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class FetchXmlCompatibilityTddTests
{
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
