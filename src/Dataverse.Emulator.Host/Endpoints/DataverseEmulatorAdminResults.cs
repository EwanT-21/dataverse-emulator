using Dataverse.Emulator.Host.Contracts;
using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace Dataverse.Emulator.Host.Endpoints;

internal static class DataverseEmulatorAdminResults
{
    public static IResult ToAdminErrorResult(IReadOnlyList<Error> errors)
        => Results.BadRequest(new EmulatorAdminErrorDescriptor(
            "invalid-request",
            errors.Select(error => new EmulatorAdminErrorItem(error.Code, error.Description)).ToArray()));
}
