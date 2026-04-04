using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Queries;

internal sealed class RetrieveMultipleXrmRequestHandler(
    DataverseXrmRecordOperations recordOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveMultiple";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var query = ResolveQuery(request);
        if (query is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Query")]);
        }

        var response = new RetrieveMultipleResponse();
        response.Results["EntityCollection"] = Invoke(ct => recordOperations.RetrieveMultipleAsync(query, ct));
        return response;
    }
}
