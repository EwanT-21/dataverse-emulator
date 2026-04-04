using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;
using OrganizationEndpointType = Microsoft.Xrm.Sdk.Organization.EndpointType;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;

internal sealed class RetrieveCurrentOrganizationXrmRequestHandler(IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveCurrentOrganization";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var response = new RetrieveCurrentOrganizationResponse();
        response.Results["Detail"] = new OrganizationDetail
        {
            OrganizationId = DataverseXrmConstants.OrganizationId,
            FriendlyName = DataverseXrmConstants.OrganizationFriendlyName,
            OrganizationVersion = DataverseXrmConstants.OrganizationVersion,
            UniqueName = DataverseXrmConstants.OrganizationUniqueName,
            UrlName = DataverseXrmConstants.OrganizationUniqueName,
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
