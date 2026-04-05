using System.ServiceModel;
using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class ExecuteMultipleXrmRequestHandler(IServiceProvider serviceProvider)
    : IXrmOrganizationRequestHandler
{
    public string RequestName => "ExecuteMultiple";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not ExecuteMultipleRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("ExecuteMultipleRequest")]);
        }

        if (typedRequest.Requests is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Requests")]);
        }

        if (typedRequest.Settings is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Settings")]);
        }

        var responseItems = new ExecuteMultipleResponseItemCollection();
        var dispatcher = serviceProvider.GetRequiredService<DataverseXrmOrganizationRequestDispatcher>();

        for (var index = 0; index < typedRequest.Requests.Count; index++)
        {
            try
            {
                var childRequest = typedRequest.Requests[index];
                if (childRequest is null)
                {
                    throw DataverseProtocolErrorMapper.ToFaultException(
                        [DataverseXrmErrors.ParameterRequired($"Requests[{index}]")]);
                }

                var childResponse = dispatcher.Dispatch(childRequest);
                if (typedRequest.Settings.ReturnResponses)
                {
                    responseItems.Add(new ExecuteMultipleResponseItem
                    {
                        RequestIndex = index,
                        Response = childResponse
                    });
                }
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                responseItems.Add(new ExecuteMultipleResponseItem
                {
                    RequestIndex = index,
                    Fault = fault.Detail
                });

                if (!typedRequest.Settings.ContinueOnError)
                {
                    break;
                }
            }
        }

        var response = new ExecuteMultipleResponse();
        response.Results["Responses"] = responseItems;
        return response;
    }
}
