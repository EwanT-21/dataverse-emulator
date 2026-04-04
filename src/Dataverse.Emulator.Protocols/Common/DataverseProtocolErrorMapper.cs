using CoreWCF;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Common;

internal static class DataverseProtocolErrorMapper
{
    public static Dictionary<string, object?> ToWebApiPayload(IReadOnlyList<Error> errors)
    {
        var primary = GetPrimaryError(errors);

        return new Dictionary<string, object?>
        {
            ["error"] = new Dictionary<string, object?>
            {
                ["code"] = primary.Code,
                ["message"] = primary.Description,
                ["details"] = errors
                    .Skip(1)
                    .Select(error => new Dictionary<string, object?>
                    {
                        ["code"] = error.Code,
                        ["message"] = error.Description
                    })
                    .ToArray()
            }
        };
    }

    public static int MapHttpStatusCode(ErrorType errorType)
        => errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

    public static FaultException<OrganizationServiceFault> ToFaultException(IReadOnlyList<Error> errors)
    {
        var primary = GetPrimaryError(errors);
        var fault = new OrganizationServiceFault
        {
            ErrorCode = MapFaultErrorCode(primary.Type),
            Message = primary.Description,
            Timestamp = DateTime.UtcNow,
            TraceText = string.Join(
                Environment.NewLine,
                errors.Select(error => $"[{error.Type}] {error.Code}: {error.Description}"))
        };

        fault.ErrorDetails.Add("DataverseEmulator.ErrorCode", primary.Code);
        fault.ErrorDetails.Add("DataverseEmulator.ErrorType", primary.Type.ToString());

        for (var index = 1; index < errors.Count; index++)
        {
            fault.ErrorDetails.Add(
                $"DataverseEmulator.Detail.{index}",
                $"{errors[index].Code}: {errors[index].Description}");
        }

        return new FaultException<OrganizationServiceFault>(fault, primary.Description);
    }

    private static Error GetPrimaryError(IReadOnlyList<Error> errors)
        => errors.Count > 0
            ? errors[0]
            : Error.Unexpected(
                code: "Protocol.Error.Unknown",
                description: "The emulator reported an unknown protocol error.");

    private static int MapFaultErrorCode(ErrorType errorType)
        => errorType switch
        {
            ErrorType.Validation => unchecked((int)0x80040203),
            ErrorType.NotFound => unchecked((int)0x80040217),
            ErrorType.Conflict => unchecked((int)0x80040237),
            _ => unchecked((int)0x80040265)
        };
}
