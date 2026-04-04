using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Execution;

public interface IXrmOrganizationRequestHandler
{
    string RequestName { get; }

    OrganizationResponse Handle(OrganizationRequest request);
}
