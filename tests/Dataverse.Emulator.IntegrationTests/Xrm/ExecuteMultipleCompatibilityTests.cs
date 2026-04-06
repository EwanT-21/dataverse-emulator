using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class ExecuteMultipleCompatibilityTests
{
    [Fact]
    public async Task ExecuteMultiple_ContinueOnError_Processes_Later_Requests_And_Captures_The_Fault()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new ExecuteMultipleRequest
        {
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = true,
                ReturnResponses = true
            },
            Requests =
            [
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Alpha",
                        ["accountnumber"] = "A-100"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["accountnumber"] = "INVALID"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Bravo",
                        ["accountnumber"] = "B-200"
                    }
                }
            ]
        };

        var response = (ExecuteMultipleResponse)context.OrganizationService.Execute(request);

        Assert.Equal(3, response.Responses.Count);
        Assert.Null(response.Responses[0].Fault);
        Assert.NotNull(response.Responses[1].Fault);
        Assert.Null(response.Responses[2].Fault);

        var verifyQuery = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        verifyQuery.Criteria.AddCondition("name", ConditionOperator.In, "Alpha", "Bravo");
        verifyQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

        var created = context.OrganizationService.RetrieveMultiple(verifyQuery);

        Assert.Equal(["Alpha", "Bravo"], created.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray());
    }

    [Fact]
    public async Task ExecuteMultiple_Stops_After_The_First_Fault_When_ContinueOnError_Is_Disabled()
    {
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var request = new ExecuteMultipleRequest
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
                        ["name"] = "Alpha",
                        ["accountnumber"] = "A-100"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["accountnumber"] = "INVALID"
                    }
                },
                new CreateRequest
                {
                    Target = new Entity("account")
                    {
                        ["name"] = "Bravo",
                        ["accountnumber"] = "B-200"
                    }
                }
            ]
        };

        var response = (ExecuteMultipleResponse)context.OrganizationService.Execute(request);

        Assert.Equal(2, response.Responses.Count);
        Assert.Null(response.Responses[0].Fault);
        Assert.NotNull(response.Responses[1].Fault);

        var verifyQuery = new QueryExpression("account")
        {
            ColumnSet = new ColumnSet("name")
        };
        verifyQuery.Criteria.AddCondition("name", ConditionOperator.In, "Alpha", "Bravo");
        verifyQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

        var created = context.OrganizationService.RetrieveMultiple(verifyQuery);

        Assert.Equal(["Alpha"], created.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray());
    }
}
