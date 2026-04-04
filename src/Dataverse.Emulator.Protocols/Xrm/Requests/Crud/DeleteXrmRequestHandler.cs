using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Crud;

internal sealed class DeleteXrmRequestHandler(
    DataverseXrmRecordOperations recordOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "Delete";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        var target = ResolveTargetReference(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        Invoke(ct => recordOperations.DeleteAsync(target.LogicalName, target.Id, ct));
        return new DeleteResponse();
    }
}
