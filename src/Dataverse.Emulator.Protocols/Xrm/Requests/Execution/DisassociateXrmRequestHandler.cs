using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class DisassociateXrmRequestHandler(
    DataverseXrmRelationshipOperations relationshipOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "Disassociate";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not DisassociateRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("DisassociateRequest")]);
        }

        Invoke(ct => relationshipOperations.DisassociateAsync(
            typedRequest.Target?.LogicalName ?? string.Empty,
            typedRequest.Target?.Id ?? Guid.Empty,
            typedRequest.Relationship!,
            typedRequest.RelatedEntities!,
            ct));

        return new DisassociateResponse();
    }
}
