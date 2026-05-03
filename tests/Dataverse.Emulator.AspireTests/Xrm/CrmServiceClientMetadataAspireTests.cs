namespace Dataverse.Emulator.AspireTests;

public sealed class CrmServiceClientMetadataAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task CrmServiceClient_Can_Read_Seeded_Metadata()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("metadata");

        Assert.Equal("account", result.GetProperty("entityLogicalName").GetString());
        Assert.Equal("accounts", result.GetProperty("entitySetName").GetString());
        Assert.Equal("accountid", result.GetProperty("primaryIdAttribute").GetString());
        Assert.Equal("name", result.GetProperty("primaryNameAttribute").GetString());
        Assert.Equal(5, result.GetProperty("attributeCount").GetInt32());
        Assert.Equal(1, result.GetProperty("objectTypeCode").GetInt32());
        Assert.Equal("name", result.GetProperty("attributeLogicalName").GetString());
        Assert.Equal("String", result.GetProperty("attributeType").GetString());
        Assert.Equal("ApplicationRequired", result.GetProperty("attributeRequiredLevel").GetString());
        Assert.Equal(2, result.GetProperty("allEntitiesCount").GetInt32());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Bounded_Metadata_Changes()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("metadata-changes");

        Assert.Equal(1, result.GetProperty("entityCount").GetInt32());
        Assert.Equal(["account"], result.ReadStringArray("entityLogicalNames"));
        Assert.Equal(["name"], result.ReadStringArray("attributeNames"));
        Assert.Equal(["contact_customer_accounts"], result.ReadStringArray("relationshipNames"));
        Assert.True(result.GetProperty("serverVersionStampPresent").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Associate_And_Disassociate_Seeded_Lookup_Relationship()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("associate");

        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("accountId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("contactId").GetString()));
        Assert.Equal(result.GetProperty("accountId").GetString(), result.GetProperty("associatedParentId").GetString());
        Assert.Equal("account", result.GetProperty("associatedParentLogicalName").GetString());
        Assert.False(result.GetProperty("disassociatedParentPresent").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Seeded_Relationship_Metadata()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("relationship-metadata");

        Assert.Equal("contact_customer_accounts", result.GetProperty("schemaName").GetString());
        Assert.Equal("account", result.GetProperty("referencedEntity").GetString());
        Assert.Equal("contact", result.GetProperty("referencingEntity").GetString());
        Assert.Equal("parentcustomerid", result.GetProperty("referencingAttribute").GetString());
        Assert.Equal(1, result.GetProperty("accountOneToManyCount").GetInt32());
        Assert.Equal(["contact_customer_accounts"], result.ReadStringArray("accountOneToManyNames"));
        Assert.Equal(1, result.GetProperty("contactManyToOneCount").GetInt32());
        Assert.Equal(["contact_customer_accounts"], result.ReadStringArray("contactManyToOneNames"));
    }
}
