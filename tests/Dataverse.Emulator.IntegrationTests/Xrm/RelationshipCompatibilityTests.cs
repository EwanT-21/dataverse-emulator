using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class RelationshipCompatibilityTests
{
    [Fact]
    public async Task Associate_And_Disassociate_Can_Target_Multiple_Contacts_Through_The_Seeded_Lookup_Relationship()
    {
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var aliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var biancaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(
                ProtocolTestMetadataFactory.CreateAccountRecord(accountId, "Alpha Account", "A-100"),
                ProtocolTestMetadataFactory.CreateContactRecord(aliceId, "Alice Alpha"),
                ProtocolTestMetadataFactory.CreateContactRecord(biancaId, "Bianca Bravo")));

        context.OrganizationService.Associate(
            "account",
            accountId,
            new Relationship("contact_customer_accounts"),
            [
                new EntityReference("contact", aliceId),
                new EntityReference("contact", biancaId)
            ]);

        var afterAssociate = context.OrganizationService.RetrieveMultiple(new QueryExpression("contact")
        {
            ColumnSet = new ColumnSet("fullname", "parentcustomerid")
        });

        Assert.All(
            afterAssociate.Entities,
            entity => Assert.Equal(accountId, entity.GetAttributeValue<EntityReference>("parentcustomerid")?.Id));

        context.OrganizationService.Disassociate(
            "account",
            accountId,
            new Relationship("contact_customer_accounts"),
            [
                new EntityReference("contact", aliceId)
            ]);

        var contactAfterDisassociate = context.OrganizationService.Retrieve(
            "contact",
            aliceId,
            new ColumnSet("parentcustomerid"));
        var otherContact = context.OrganizationService.Retrieve(
            "contact",
            biancaId,
            new ColumnSet("parentcustomerid"));

        Assert.Null(contactAfterDisassociate.GetAttributeValue<EntityReference>("parentcustomerid"));
        Assert.Equal(accountId, otherContact.GetAttributeValue<EntityReference>("parentcustomerid")?.Id);
    }
}
