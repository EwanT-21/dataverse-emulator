using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;

internal sealed class WhoAmIXrmRequestHandler(IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "WhoAmI";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var response = new WhoAmIResponse();
        response.Results["UserId"] = DataverseXrmConstants.DefaultUserId;
        response.Results["BusinessUnitId"] = DataverseXrmConstants.DefaultBusinessUnitId;
        response.Results["OrganizationId"] = DataverseXrmConstants.OrganizationId;
        return response;
    }
}
