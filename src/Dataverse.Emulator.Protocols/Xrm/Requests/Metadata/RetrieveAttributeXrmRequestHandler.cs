using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;

internal sealed class RetrieveAttributeXrmRequestHandler(
    DataverseXrmMetadataOperations metadataOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveAttribute";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveAttributeRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveAttributeRequest")]);
        }

        var response = new RetrieveAttributeResponse();
        response.Results["AttributeMetadata"] = Invoke(ct => metadataOperations.RetrieveAttributeAsync(typedRequest, ct));
        return response;
    }
}
