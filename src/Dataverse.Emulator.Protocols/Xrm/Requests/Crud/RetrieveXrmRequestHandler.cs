using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Crud;

internal sealed class RetrieveXrmRequestHandler(
    DataverseXrmRecordOperations recordOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "Retrieve";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is RetrieveRequest { RelatedEntitiesQuery: not null })
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.UnsupportedOperation("Retrieve related entities")]);
        }

        var target = ResolveTargetReference(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        var columnSet = ResolveColumnSet(request);
        if (columnSet is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("ColumnSet")]);
        }

        var response = new RetrieveResponse();
        response.Results["Entity"] = Invoke(ct => recordOperations.RetrieveAsync(target.LogicalName, target.Id, columnSet, ct));
        return response;
    }
}
