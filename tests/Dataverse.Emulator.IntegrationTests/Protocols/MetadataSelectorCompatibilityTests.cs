using Dataverse.Emulator.Protocols.Xrm.Metadata;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class MetadataSelectorCompatibilityTests
{
    [Fact]
    public async Task RetrieveEntity_By_MetadataId_Returns_The_Seeded_Table()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveEntityRequest
        {
            MetadataId = DataverseXrmMetadataMapper.CreateTableMetadataId("account"),
            EntityFilters = EntityFilters.Entity | EntityFilters.Attributes
        };

        var response = (RetrieveEntityResponse)context.OrganizationService.Execute(request);
        var metadata = response.EntityMetadata;

        Assert.Equal("account", metadata.LogicalName);
        Assert.Equal("accounts", metadata.EntitySetName);
        Assert.Contains(metadata.Attributes, attribute => attribute.LogicalName == "name");
    }

    [Fact]
    public async Task RetrieveAttribute_By_MetadataId_Returns_The_Seeded_Attribute()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveAttributeRequest
        {
            MetadataId = DataverseXrmMetadataMapper.CreateColumnMetadataId("account", "name")
        };

        var response = (RetrieveAttributeResponse)context.OrganizationService.Execute(request);
        var metadata = response.AttributeMetadata;

        Assert.Equal("name", metadata.LogicalName);
        Assert.Equal(AttributeTypeCode.String, metadata.AttributeType);
        Assert.Equal("account", metadata.EntityLogicalName);
    }

    [Fact]
    public async Task RetrieveRelationship_By_MetadataId_Returns_The_Seeded_Lookup_Relationship()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveRelationshipRequest
        {
            MetadataId = DataverseXrmMetadataMapper.CreateRelationshipMetadataId("contact_customer_accounts")
        };

        var response = (RetrieveRelationshipResponse)context.OrganizationService.Execute(request);
        var metadata = Assert.IsType<OneToManyRelationshipMetadata>(response.RelationshipMetadata);

        Assert.Equal("contact_customer_accounts", metadata.SchemaName);
        Assert.Equal("account", metadata.ReferencedEntity);
        Assert.Equal("contact", metadata.ReferencingEntity);
    }

    [Fact]
    public async Task RetrieveAllEntities_With_Relationships_Returns_The_Seeded_Relational_Shape()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveAllEntitiesRequest
        {
            EntityFilters = EntityFilters.Entity | EntityFilters.Relationships
        };

        var response = (RetrieveAllEntitiesResponse)context.OrganizationService.Execute(request);

        var account = response.EntityMetadata.Single(entity => entity.LogicalName == "account");
        var contact = response.EntityMetadata.Single(entity => entity.LogicalName == "contact");

        Assert.Contains(account.OneToManyRelationships, relationship => relationship.SchemaName == "contact_customer_accounts");
        Assert.Contains(contact.ManyToOneRelationships, relationship => relationship.SchemaName == "contact_customer_accounts");
    }
}
