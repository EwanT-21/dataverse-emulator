using CoreWCF;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class OrganizationRequestCompatibilityTddTests
{
    [Fact]
    public async Task ExecuteTransaction_Commits_Batched_Create_Requests_Atomically()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new ExecuteTransactionRequest
        {
            ReturnResponses = true,
            Requests =
            [
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Transactional Alpha",
                        ["accountnumber"] = "TA-100"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Transactional Bravo",
                        ["accountnumber"] = "TB-200"
                    }
                }
            ]
        };

        var response = (ExecuteTransactionResponse)context.OrganizationService.Execute(request);

        Assert.Equal(2, response.Responses.Count);

        var verifyQuery = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name", "accountnumber")
        };
        verifyQuery.Criteria.AddCondition("name", ConditionOperator.In, "Transactional Alpha", "Transactional Bravo");
        verifyQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

        var created = context.OrganizationService.RetrieveMultiple(verifyQuery);

        Assert.Equal(["Transactional Alpha", "Transactional Bravo"], created.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray());
    }

    [Fact]
    public async Task ExecuteTransaction_Rolls_Back_The_Batch_When_A_Child_Request_Fails()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new ExecuteTransactionRequest
        {
            ReturnResponses = true,
            Requests =
            [
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Should Roll Back",
                        ["accountnumber"] = "RB-100"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["accountnumber"] = "RB-INVALID"
                    }
                }
            ]
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () => context.OrganizationService.Execute(request));

        Assert.Contains("name", fault.Detail.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, fault.Detail.ErrorDetails["DataverseEmulator.ExecuteTransaction.FaultedRequestIndex"]);

        var verifyQuery = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        verifyQuery.Criteria.AddCondition("name", ConditionOperator.Equal, "Should Roll Back");

        var created = context.OrganizationService.RetrieveMultiple(verifyQuery);

        Assert.Empty(created.Entities);
    }

    [Fact]
    public async Task ExecuteTransaction_With_Nested_Batch_Request_Faults_And_Rolls_Back_Earlier_Changes()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new ExecuteTransactionRequest
        {
            ReturnResponses = true,
            Requests =
            [
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Should Not Persist",
                        ["accountnumber"] = "NB-100"
                    }
                },
                new ExecuteMultipleRequest
                {
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = true
                    },
                    Requests =
                    [
                        new CreateRequest
                        {
                            Target = new Entity("account")
                            {
                                ["name"] = "Nested",
                                ["accountnumber"] = "NB-200"
                            }
                        }
                    ]
                }
            ]
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () => context.OrganizationService.Execute(request));

        Assert.Contains("ExecuteMultiple", fault.Detail.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, fault.Detail.ErrorDetails["DataverseEmulator.ExecuteTransaction.FaultedRequestIndex"]);

        var verifyQuery = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        verifyQuery.Criteria.AddCondition("name", ConditionOperator.Equal, "Should Not Persist");

        var created = context.OrganizationService.RetrieveMultiple(verifyQuery);

        Assert.Empty(created.Entities);
    }

    [Fact]
    public async Task RetrieveMetadataChanges_Returns_Seeded_Entity_And_Attribute_Shape()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveMetadataChangesRequest
        {
            Query = new EntityQueryExpression
            {
                Criteria = new MetadataFilterExpression(LogicalOperator.And),
                Properties = new MetadataPropertiesExpression("LogicalName", "PrimaryIdAttribute", "PrimaryNameAttribute"),
                AttributeQuery = new AttributeQueryExpression
                {
                    Properties = new MetadataPropertiesExpression("LogicalName", "AttributeType")
                },
                LabelQuery = new LabelQueryExpression()
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var response = (RetrieveMetadataChangesResponse)context.OrganizationService.Execute(request);

        Assert.Contains(response.EntityMetadata, entity => entity.LogicalName == "account");
        Assert.Contains(response.EntityMetadata, entity => entity.LogicalName == "contact");
        Assert.Contains(
            response.EntityMetadata.Single(entity => entity.LogicalName == "account").Attributes,
            attribute => attribute.LogicalName == "name");
        Assert.False(string.IsNullOrWhiteSpace(response.ServerVersionStamp));
    }

    [Fact]
    public async Task RetrieveMetadataChanges_Can_Return_Seeded_Relationship_Metadata_When_Requested()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var criteria = new MetadataFilterExpression(LogicalOperator.And);
        criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, "account"));

        var request = new RetrieveMetadataChangesRequest
        {
            Query = new EntityQueryExpression
            {
                Criteria = criteria,
                Properties = new MetadataPropertiesExpression("LogicalName"),
                RelationshipQuery = new RelationshipQueryExpression
                {
                    Properties = new MetadataPropertiesExpression("SchemaName", "ReferencedEntity", "ReferencingEntity")
                }
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var response = (RetrieveMetadataChangesResponse)context.OrganizationService.Execute(request);
        var account = response.EntityMetadata.Single(entity => entity.LogicalName == "account");

        Assert.Contains(
            account.OneToManyRelationships,
            relationship => relationship.SchemaName == "contact_customer_accounts");
    }

    [Fact]
    public async Task RetrieveMetadataChanges_With_Unsupported_Metadata_Selectors_Faults_Clearly()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var criteria = new MetadataFilterExpression(LogicalOperator.And);
        criteria.Conditions.Add(new MetadataConditionExpression("IsIntersect", MetadataConditionOperator.Equals, false));

        var request = new RetrieveMetadataChangesRequest
        {
            Query = new EntityQueryExpression
            {
                Criteria = criteria,
                Properties = new MetadataPropertiesExpression("LogicalName")
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () => context.OrganizationService.Execute(request));

        Assert.Contains("metadata property 'IsIntersect'", fault.Detail.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RetrieveMetadataChanges_With_Unsupported_Attribute_Query_Criteria_Faults_Clearly()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveMetadataChangesRequest
        {
            Query = new EntityQueryExpression
            {
                Properties = new MetadataPropertiesExpression("LogicalName"),
                AttributeQuery = new AttributeQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, "name")
                        }
                    },
                    Properties = new MetadataPropertiesExpression("LogicalName")
                }
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () => context.OrganizationService.Execute(request));

        Assert.Contains("AttributeQuery.Criteria", fault.Detail.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RetrieveMetadataChanges_With_Unsupported_Relationship_Query_Criteria_Faults_Clearly()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new RetrieveMetadataChangesRequest
        {
            Query = new EntityQueryExpression
            {
                Properties = new MetadataPropertiesExpression("LogicalName"),
                RelationshipQuery = new RelationshipQueryExpression
                {
                    Criteria = new MetadataFilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new MetadataConditionExpression("SchemaName", MetadataConditionOperator.Equals, "contact_customer_accounts")
                        }
                    },
                    Properties = new MetadataPropertiesExpression("SchemaName")
                }
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () => context.OrganizationService.Execute(request));

        Assert.Contains("RelationshipQuery.Criteria", fault.Detail.Message, StringComparison.OrdinalIgnoreCase);
    }
}
