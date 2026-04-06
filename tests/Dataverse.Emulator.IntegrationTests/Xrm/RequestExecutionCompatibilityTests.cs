using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class RequestExecutionCompatibilityTests
{
    [Fact]
    public async Task RetrieveMultipleRequest_Can_Execute_A_QueryExpression_Through_The_Request_Dispatcher()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Alpha Account",
                    "A-100"),
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Bravo Account",
                    "B-200")));

        var request = new RetrieveMultipleRequest
        {
            Query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "accountnumber"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, "Alpha Account")
                    }
                }
            }
        };

        var response = (RetrieveMultipleResponse)context.OrganizationService.Execute(request);

        Assert.Single(response.EntityCollection.Entities);
        Assert.Equal("Alpha Account", response.EntityCollection.Entities[0].GetAttributeValue<string>("name"));
        Assert.Equal("A-100", response.EntityCollection.Entities[0].GetAttributeValue<string>("accountnumber"));
    }

    [Fact]
    public async Task RetrieveMultipleRequest_Can_Execute_A_FetchExpression_Through_The_Request_Dispatcher()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alpha",
                    "A-100"),
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Bravo",
                    "B-200")));

        var request = new RetrieveMultipleRequest
        {
            Query = new FetchExpression(
                "<fetch>" +
                "<entity name='account'>" +
                "<attribute name='name' />" +
                "<filter>" +
                "<condition attribute='accountnumber' operator='eq' value='B-200' />" +
                "</filter>" +
                "</entity>" +
                "</fetch>")
        };

        var response = (RetrieveMultipleResponse)context.OrganizationService.Execute(request);

        Assert.Single(response.EntityCollection.Entities);
        Assert.Equal("Bravo", response.EntityCollection.Entities[0].GetAttributeValue<string>("name"));
    }

    [Fact]
    public async Task Runtime_Organization_Requests_Return_The_Seeded_Compatibility_Profile()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var versionResponse = (RetrieveVersionResponse)context.OrganizationService.Execute(new RetrieveVersionRequest());
        var installedLanguagePacksResponse = (RetrieveInstalledLanguagePacksResponse)context.OrganizationService.Execute(new RetrieveInstalledLanguagePacksRequest());
        var organizationInfoResponse = (RetrieveOrganizationInfoResponse)context.OrganizationService.Execute(new RetrieveOrganizationInfoRequest());

        var installedLanguagePacks = Assert.IsType<int[]>(installedLanguagePacksResponse.Results["RetrieveInstalledLanguagePacks"]);
        var organizationInfo = Assert.IsType<OrganizationInfo>(organizationInfoResponse.Results["organizationInfo"]);

        Assert.Equal("9.2.0.0", versionResponse.Version);
        Assert.Equal([1033], installedLanguagePacks);
        Assert.Equal(OrganizationType.Developer, organizationInfo.InstanceType);
        Assert.Empty(organizationInfo.Solutions);
    }
}
