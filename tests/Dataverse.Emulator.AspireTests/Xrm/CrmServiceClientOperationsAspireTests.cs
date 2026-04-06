namespace Dataverse.Emulator.AspireTests;

public sealed class CrmServiceClientOperationsAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task CrmServiceClient_Crud_And_QueryExpression_Flow_Works()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("crud");

        Assert.Equal("Contoso", result.GetProperty("retrievedName").GetString());
        Assert.Equal("A-100", result.GetProperty("retrievedAccountNumber").GetString());
        Assert.Equal("A-200", result.GetProperty("updatedAccountNumber").GetString());
        Assert.Equal(1, result.GetProperty("queryCount").GetInt32());
        Assert.False(result.GetProperty("moreRecords").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_QueryExpression_Paging_RoundTrips()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("paged-query");

        Assert.Equal("Alpha", result.GetProperty("firstPageName").GetString());
        Assert.Equal(1, result.GetProperty("firstPageCount").GetInt32());
        Assert.True(result.GetProperty("firstMoreRecords").GetBoolean());
        Assert.True(result.GetProperty("firstPagingCookiePresent").GetBoolean());
        Assert.Equal("Bravo", result.GetProperty("secondPageName").GetString());
        Assert.Equal(1, result.GetProperty("secondPageCount").GetInt32());
    }

    [Fact]
    public async Task CrmServiceClient_Advanced_QueryExpression_Filters_RoundTrip()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("advanced-query");

        Assert.Equal(2, result.GetProperty("groupedCount").GetInt32());
        Assert.Equal(["Alpha", "Charlie"], result.ReadStringArray("groupedNames"));
        Assert.Equal(2, result.GetProperty("inCount").GetInt32());
        Assert.Equal(["Alpha", "Charlie"], result.ReadStringArray("inNames"));
        Assert.Equal(2, result.GetProperty("likeCount").GetInt32());
        Assert.Equal(["Alpha", "Alpine"], result.ReadStringArray("likeNames"));
        Assert.Equal(2, result.GetProperty("rangeCount").GetInt32());
        Assert.Equal(["Alpine", "Bravo"], result.ReadStringArray("rangeNames"));
    }

    [Fact]
    public async Task CrmServiceClient_Linked_QueryExpression_RoundTrips_Across_Tables()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("linked-query");

        Assert.Equal(2, result.GetProperty("count").GetInt32());
        Assert.Equal(["Alice Alpha", "Aria Alpha"], result.ReadStringArray("names"));
        Assert.Equal(["Alpha Account", "Alpha Account"], result.ReadStringArray("accountNames"));
        Assert.Equal(["A-100", "A-100"], result.ReadStringArray("accountNumbers"));
    }

    [Fact]
    public async Task CrmServiceClient_FetchXml_RetrieveMultiple_RoundTrips()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("fetchxml");

        Assert.Equal(2, result.GetProperty("firstPageCount").GetInt32());
        Assert.Equal(["Alpha", "Alpine"], result.ReadStringArray("firstPageNames"));
        Assert.True(result.GetProperty("firstMoreRecords").GetBoolean());
        Assert.True(result.GetProperty("firstPagingCookiePresent").GetBoolean());
        Assert.Equal(1, result.GetProperty("secondPageCount").GetInt32());
        Assert.Equal(["Charlie"], result.ReadStringArray("secondPageNames"));
        Assert.False(result.GetProperty("secondMoreRecords").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_ExecuteMultiple_Composes_Existing_Request_Slices()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("execute-multiple");

        Assert.False(result.GetProperty("isFaulted").GetBoolean());
        Assert.Equal(3, result.GetProperty("responseCount").GetInt32());
        Assert.Equal([0, 1, 2], result.ReadIntArray("successIndices"));
        Assert.Equal(2, result.GetProperty("createdCount").GetInt32());
        Assert.Equal(["Alpha", "Bravo"], result.ReadStringArray("createdNames"));
    }

    [Fact]
    public async Task CrmServiceClient_UpsertRequest_Composes_Create_And_Update_Flow()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("upsert");

        Assert.True(result.GetProperty("createRecordCreated").GetBoolean());
        Assert.False(result.GetProperty("updateRecordCreated").GetBoolean());
        Assert.Equal(result.GetProperty("createdId").GetString(), result.GetProperty("updateTargetId").GetString());
        Assert.Equal("Upserted", result.GetProperty("retrievedName").GetString());
        Assert.Equal("UP-200", result.GetProperty("retrievedAccountNumber").GetString());
    }
}
