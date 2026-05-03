using CoreWCF;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class ExecuteTransactionXrmRequestHandler(IServiceProvider serviceProvider)
    : IXrmOrganizationRequestHandler
{
    public string RequestName => "ExecuteTransaction";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not ExecuteTransactionRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("ExecuteTransactionRequest")]);
        }

        if (typedRequest.Requests is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Requests")]);
        }

        var snapshotService = serviceProvider.GetRequiredService<IEmulatorStateSnapshotService>();
        var dispatcher = serviceProvider.GetRequiredService<DataverseXrmOrganizationRequestDispatcher>();

        var snapshot = snapshotService.CaptureAsync().GetAwaiter().GetResult();

        var responses = new OrganizationResponseCollection();

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

                if (IsUnsupportedNestedBatch(childRequest))
                {
                    throw DataverseProtocolErrorMapper.ToFaultException(
                        [DataverseXrmErrors.UnsupportedOperation(
                            $"ExecuteTransaction child request '{childRequest.RequestName ?? childRequest.GetType().Name}'")]);
                }

                var childResponse = dispatcher.Dispatch(childRequest);
                if (typedRequest.ReturnResponses == true)
                {
                    responses.Add(childResponse);
                }
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                RestoreSnapshot(snapshotService, snapshot);
                throw CreateExecuteTransactionFault(fault, index);
            }
        }

        var response = new ExecuteTransactionResponse();
        response.Results["Responses"] = responses;
        return response;
    }

    private static bool IsUnsupportedNestedBatch(OrganizationRequest childRequest)
        => childRequest is ExecuteTransactionRequest or ExecuteMultipleRequest;

    private static void RestoreSnapshot(
        IEmulatorStateSnapshotService snapshotService,
        EmulatorStateSnapshot snapshot)
    {
        snapshotService.RestoreAsync(snapshot).GetAwaiter().GetResult();
    }

    private static FaultException<OrganizationServiceFault> CreateExecuteTransactionFault(
        FaultException<OrganizationServiceFault> fault,
        int faultedRequestIndex)
    {
        var executeTransactionFault = new ExecuteTransactionFault
        {
            ErrorCode = fault.Detail.ErrorCode,
            Message = fault.Detail.Message,
            Timestamp = fault.Detail.Timestamp,
            TraceText = fault.Detail.TraceText,
            InnerFault = fault.Detail.InnerFault,
            FaultedRequestIndex = faultedRequestIndex
        };

        foreach (var pair in fault.Detail.ErrorDetails)
        {
            executeTransactionFault.ErrorDetails[pair.Key] = pair.Value;
        }

        executeTransactionFault.ErrorDetails["DataverseEmulator.ExecuteTransaction.FaultedRequestIndex"] = faultedRequestIndex;

        return new FaultException<OrganizationServiceFault>(
            executeTransactionFault,
            executeTransactionFault.Message);
    }
}
