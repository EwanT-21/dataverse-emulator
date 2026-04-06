using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class QueryByAttributeCompatibilityTddTests
{
    [Fact]
    public async Task QueryByAttribute_Can_Filter_Records_Through_The_Shared_Query_Path()
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

        var query = new QueryByAttribute("account")
        {
            ColumnSet = new ColumnSet("name", "accountnumber")
        };
        query.Attributes.Add("accountnumber");
        query.Values.Add("A-100");
        query.Orders.Add(new OrderExpression("name", OrderType.Ascending));

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Entities);
        Assert.Equal("Alpha Account", result.Value.Entities[0].GetAttributeValue<string>("name"));
        Assert.Equal("A-100", result.Value.Entities[0].GetAttributeValue<string>("accountnumber"));
    }

    [Fact]
    public async Task QueryByAttribute_Can_Compose_TopCount_And_Paging()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "Alpha",
                    "A-100",
                    isActive: true),
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    "Alpine",
                    "A-200",
                    isActive: true),
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    "Atlas",
                    "A-300",
                    isActive: true)));

        var query = new QueryByAttribute("account")
        {
            ColumnSet = new ColumnSet("name"),
            PageInfo = new PagingInfo
            {
                Count = 1,
                PageNumber = 1
            },
            TopCount = 2
        };
        query.Attributes.Add("isactive");
        query.Values.Add(true);
        query.Orders.Add(new OrderExpression("name", OrderType.Ascending));

        var result = await context.RecordOperations.RetrieveMultipleAsync(query, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Single(result.Value.Entities);
        Assert.Equal("Alpha", result.Value.Entities[0].GetAttributeValue<string>("name"));
        Assert.True(result.Value.MoreRecords);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.PagingCookie));
    }

    [Fact]
    public async Task QueryByAttribute_Can_Run_Through_The_Public_OrganizationService_Surface()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Alpha Account",
                    "A-100",
                    isActive: true),
                ProtocolTestMetadataFactory.CreateAccountRecord(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Alpha Account Inactive",
                    "A-100",
                    isActive: false)));

        var query = new QueryByAttribute("account")
        {
            ColumnSet = new ColumnSet("name", "accountnumber", "isactive")
        };
        query.Attributes.Add("accountnumber");
        query.Values.Add("A-100");
        query.Attributes.Add("isactive");
        query.Values.Add(true);

        var result = context.OrganizationService.RetrieveMultiple(query);

        Assert.Single(result.Entities);
        Assert.Equal("Alpha Account", result.Entities[0].GetAttributeValue<string>("name"));
        Assert.True(result.Entities[0].GetAttributeValue<bool>("isactive"));
    }
}
