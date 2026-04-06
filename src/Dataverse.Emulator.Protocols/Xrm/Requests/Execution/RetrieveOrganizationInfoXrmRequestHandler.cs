using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Organization;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveOrganizationInfoXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveOrganizationInfo";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveOrganizationInfoRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveOrganizationInfoRequest")]);
        }

        var profile = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct));
        var response = new RetrieveOrganizationInfoResponse();
        response.Results["organizationInfo"] = new OrganizationInfo
        {
            InstanceType = ResolveOrganizationType(profile.OrganizationTypeName),
            Solutions = profile.SolutionUniqueNames
                .Select(solutionUniqueName => new Solution
                {
                    SolutionUniqueName = solutionUniqueName,
                    FriendlyName = solutionUniqueName
                })
                .ToList()
        };
        return response;
    }

    private static OrganizationType ResolveOrganizationType(string organizationTypeName)
        => Enum.TryParse<OrganizationType>(organizationTypeName, ignoreCase: true, out var organizationType)
            ? organizationType
            : OrganizationType.Developer;
}
