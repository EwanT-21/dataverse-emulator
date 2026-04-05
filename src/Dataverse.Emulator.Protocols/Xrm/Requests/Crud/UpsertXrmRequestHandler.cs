using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Crud;

internal sealed class UpsertXrmRequestHandler(
    DataverseXrmRecordOperations recordOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "Upsert";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not UpsertRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("UpsertRequest")]);
        }

        var target = typedRequest.Target;
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        var result = Invoke(ct => recordOperations.UpsertAsync(target, ct));

        var response = new UpsertResponse();
        response.Results["RecordCreated"] = result.RecordCreated;
        response.Results["Target"] = new EntityReference(target.LogicalName, result.Id);
        return response;
    }
}
