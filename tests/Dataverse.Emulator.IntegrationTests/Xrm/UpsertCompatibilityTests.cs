using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class UpsertCompatibilityTests
{
    [Fact]
    public async Task Upsert_Can_Use_The_PrimaryId_Attribute_When_EntityId_Is_Not_Set()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var createResponse = (UpsertResponse)context.OrganizationService.Execute(
            new UpsertRequest
            {
                Target = new Entity("account")
                {
                    ["accountid"] = accountId,
                    ["name"] = "Primary Key Create",
                    ["accountnumber"] = "PK-100"
                }
            });

        var updateResponse = (UpsertResponse)context.OrganizationService.Execute(
            new UpsertRequest
            {
                Target = new Entity("account")
                {
                    ["accountid"] = accountId,
                    ["name"] = "Primary Key Update",
                    ["accountnumber"] = "PK-200"
                }
            });

        var retrieved = context.OrganizationService.Retrieve(
            "account",
            accountId,
            new ColumnSet("name", "accountnumber"));

        Assert.True(createResponse.RecordCreated);
        Assert.False(updateResponse.RecordCreated);
        Assert.Equal(accountId, createResponse.Target.Id);
        Assert.Equal(accountId, updateResponse.Target.Id);
        Assert.Equal("Primary Key Update", retrieved.GetAttributeValue<string>("name"));
        Assert.Equal("PK-200", retrieved.GetAttributeValue<string>("accountnumber"));
    }
}
