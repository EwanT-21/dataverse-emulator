using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;

internal sealed class WhoAmIXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "WhoAmI";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var profile = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct));
        var response = new WhoAmIResponse();
        response.Results["UserId"] = profile.DefaultUserId;
        response.Results["BusinessUnitId"] = profile.DefaultBusinessUnitId;
        response.Results["OrganizationId"] = profile.OrganizationId;
        return response;
    }
}
