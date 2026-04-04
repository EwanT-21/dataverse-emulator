using Dataverse.Emulator.Protocols.Common;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Execution;

public sealed class DataverseXrmOrganizationRequestDispatcher(
    IEnumerable<IXrmOrganizationRequestHandler> handlers)
{
    private readonly IReadOnlyDictionary<string, IXrmOrganizationRequestHandler> handlersByName = handlers
        .ToDictionary(handler => handler.RequestName, StringComparer.OrdinalIgnoreCase);

    public OrganizationResponse Dispatch(OrganizationRequest request)
    {
        if (request is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("request")]);
        }

        var requestName = request.RequestName ?? request.GetType().Name;
        if (!handlersByName.TryGetValue(requestName, out var handler))
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.UnsupportedOrganizationRequest(requestName)]);
        }

        return handler.Handle(request);
    }
}
