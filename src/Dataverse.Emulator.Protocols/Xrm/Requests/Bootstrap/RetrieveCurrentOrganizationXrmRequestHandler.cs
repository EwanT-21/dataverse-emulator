using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using OrganizationEndpointType = Microsoft.Xrm.Sdk.Organization.EndpointType;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;

internal sealed class RetrieveCurrentOrganizationXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveCurrentOrganization";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var profile = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct));
        var response = new RetrieveCurrentOrganizationResponse();
        response.Results["Detail"] = new OrganizationDetail
        {
            OrganizationId = profile.OrganizationId,
            FriendlyName = profile.OrganizationFriendlyName,
            OrganizationVersion = profile.OrganizationVersion,
            UniqueName = profile.OrganizationUniqueName,
            UrlName = profile.OrganizationUniqueName,
            State = OrganizationState.Enabled,
            Endpoints = new EndpointCollection
            {
                { OrganizationEndpointType.OrganizationService, BuildAbsoluteUri(DataverseXrmConstants.OrganizationServicePath) },
                { OrganizationEndpointType.WebApplication, BuildAbsoluteUri("/") }
            }
        };

        return response;
    }
}
