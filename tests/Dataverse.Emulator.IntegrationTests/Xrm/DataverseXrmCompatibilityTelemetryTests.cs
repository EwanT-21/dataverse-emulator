using CoreWCF;
using Dataverse.Emulator.Protocols.Common.Telemetry;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class DataverseXrmCompatibilityTelemetryTests
{
    [Fact]
    public void Classifier_Preserves_Known_Sdk_Request_Names()
    {
        var classifier = new DataverseXrmCompatibilityTelemetryClassifier();
        var fault = CreateFault(
            "Protocol.Xrm.Execute.Unsupported",
            "Organization request 'WhoAmI' is not supported by the local Dataverse emulator.");

        var compatibilityEvent = classifier.Classify("ExecuteRequest", "WhoAmI", fault);

        Assert.NotNull(compatibilityEvent);
        Assert.Equal("unsupported-capability", compatibilityEvent.EventKind);
        Assert.Equal("organization-request", compatibilityEvent.CapabilityKind);
        Assert.Equal("WhoAmI", compatibilityEvent.CapabilityKey);
    }

    [Fact]
    public async Task Unsupported_Custom_Request_Names_Are_Sanitized_Before_Telemetry_Is_Recorded()
    {
        var collector = new TestCompatibilityTelemetryCollector();
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(),
            services => services.AddSingleton<IDataverseCompatibilityTelemetry>(collector));

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () =>
            {
                context.RequestDispatcher.Dispatch(new OrganizationRequest
                {
                    RequestName = "ContosoSecretSync"
                });
            });

        Assert.Contains("ContosoSecretSync", fault.Detail.Message, StringComparison.Ordinal);
        var compatibilityEvent = Assert.Single(collector.Events);
        Assert.Equal("organization-request", compatibilityEvent.CapabilityKind);
        Assert.Equal("custom-or-unknown", compatibilityEvent.CapabilityKey);
    }

    [Fact]
    public async Task Unsupported_Metadata_Property_Records_A_Sanitized_Telemetry_Event()
    {
        var collector = new TestCompatibilityTelemetryCollector();
        await using var context = await XrmProtocolTestContext.CreateAsync(
            ProtocolTestMetadataFactory.CreateDefaultXrmScenario(),
            services => services.AddSingleton<IDataverseCompatibilityTelemetry>(collector));

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
                            new MetadataConditionExpression("DisplayName", MetadataConditionOperator.Equals, "Name")
                        }
                    },
                    Properties = new MetadataPropertiesExpression("LogicalName")
                }
            },
            DeletedMetadataFilters = DeletedMetadataFilters.Default
        };

        var fault = Assert.Throws<FaultException<OrganizationServiceFault>>(
            () =>
            {
                context.OrganizationService.Execute(request);
            });

        Assert.Contains("DisplayName", fault.Detail.Message, StringComparison.Ordinal);
        var compatibilityEvent = Assert.Single(collector.Events);
        Assert.Equal("metadata-attribute-property", compatibilityEvent.CapabilityKind);
        Assert.Equal("DisplayName", compatibilityEvent.CapabilityKey);
    }

    private static OrganizationServiceFault CreateFault(string errorCode, string message)
    {
        var fault = new OrganizationServiceFault
        {
            Message = message
        };

        fault.ErrorDetails.Add("DataverseEmulator.ErrorCode", errorCode);
        return fault;
    }

    private sealed class TestCompatibilityTelemetryCollector : IDataverseCompatibilityTelemetry
    {
        public List<DataverseCompatibilityTelemetryEvent> Events { get; } = [];

        public void Record(DataverseCompatibilityTelemetryEvent telemetryEvent)
            => Events.Add(telemetryEvent);
    }
}
