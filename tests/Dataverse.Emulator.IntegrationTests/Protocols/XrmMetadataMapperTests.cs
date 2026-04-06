using Dataverse.Emulator.Protocols.Xrm.Metadata;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class XrmMetadataMapperTests
{
    [Fact]
    public void Relationship_Metadata_Maps_To_OneToManyRelationshipMetadata()
    {
        var relationship = new Dataverse.Emulator.Domain.Metadata.LookupRelationshipDefinition(
            "contact_customer_accounts",
            "account",
            "accountid",
            "contact",
            "parentcustomerid");

        var metadata = DataverseXrmMetadataMapper.ToRelationshipMetadata(relationship);

        Assert.Equal("contact_customer_accounts", metadata.SchemaName);
        Assert.Equal("account", metadata.ReferencedEntity);
        Assert.Equal("accountid", metadata.ReferencedAttribute);
        Assert.Equal("contact", metadata.ReferencingEntity);
        Assert.Equal("parentcustomerid", metadata.ReferencingAttribute);
        Assert.Equal(
            DataverseXrmMetadataMapper.CreateRelationshipMetadataId("contact_customer_accounts"),
            metadata.MetadataId.GetValueOrDefault());
    }

    [Fact]
    public void Entity_Metadata_Includes_Lookup_Relationships_When_Requested()
    {
        var account = ProtocolTestMetadataFactory.CreateAccountTable();
        var contact = ProtocolTestMetadataFactory.CreateContactTable();
        var relationship = new Dataverse.Emulator.Domain.Metadata.LookupRelationshipDefinition(
            "contact_customer_accounts",
            "account",
            "accountid",
            "contact",
            "parentcustomerid");

        var accountMetadata = DataverseXrmMetadataMapper.ToEntityMetadata(
            account,
            EntityFilters.Entity | EntityFilters.Relationships,
            [relationship]);
        var contactMetadata = DataverseXrmMetadataMapper.ToEntityMetadata(
            contact,
            EntityFilters.Entity | EntityFilters.Relationships,
            [relationship]);

        Assert.Single(accountMetadata.OneToManyRelationships);
        Assert.Equal("contact_customer_accounts", accountMetadata.OneToManyRelationships[0].SchemaName);
        Assert.Single(contactMetadata.ManyToOneRelationships);
        Assert.Equal("contact_customer_accounts", contactMetadata.ManyToOneRelationships[0].SchemaName);
    }

    [Fact]
    public void Entity_Metadata_Maps_Primary_Information_And_Attributes()
    {
        var table = ProtocolTestMetadataFactory.CreateAccountTable();
        var metadata = DataverseXrmMetadataMapper.ToEntityMetadata(table, EntityFilters.Entity | EntityFilters.Attributes);

        Assert.Equal("account", metadata.LogicalName);
        Assert.Equal("accounts", metadata.EntitySetName);
        Assert.Equal("accountid", metadata.PrimaryIdAttribute);
        Assert.Equal("name", metadata.PrimaryNameAttribute);
        Assert.Equal(4, metadata.Attributes.Length);
        Assert.Contains(metadata.Attributes, attribute => attribute.LogicalName == "name" && attribute.IsPrimaryName == true);
    }
}
