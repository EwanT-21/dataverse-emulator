using ErrorOr;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmErrors
{
    public static Error ParameterRequired(string parameterName)
        => Error.Validation(
            code: "Protocol.Xrm.Parameter.Required",
            description: $"Parameter '{parameterName}' is required.");

    public static Error UnsupportedOperation(string operation)
        => Error.Validation(
            code: "Protocol.Xrm.Operation.Unsupported",
            description: $"Operation '{operation}' is not supported by the local Dataverse emulator.");

    public static Error UnsupportedOrganizationRequest(string requestName)
        => Error.Validation(
            code: "Protocol.Xrm.Execute.Unsupported",
            description: $"Organization request '{requestName}' is not supported by the local Dataverse emulator.");

    public static Error UnsupportedQueryType(string queryType)
        => Error.Validation(
            code: "Protocol.Xrm.Query.UnsupportedType",
            description: $"Query type '{queryType}' is not supported by the local Dataverse emulator.");

    public static Error UnsupportedQueryFeature(string feature)
        => Error.Validation(
            code: "Protocol.Xrm.Query.Unsupported",
            description: $"QueryExpression feature '{feature}' is not supported by the local Dataverse emulator.");

    public static Error InvalidFetchXml(string message)
        => Error.Validation(
            code: "Protocol.Xrm.FetchXml.Invalid",
            description: message);

    public static Error UnsupportedFetchXmlFeature(string feature)
        => Error.Validation(
            code: "Protocol.Xrm.FetchXml.Unsupported",
            description: $"FetchXML feature '{feature}' is not supported by the local Dataverse emulator.");

    public static Error InvalidPagingRequest(string message)
        => Error.Validation(
            code: "Protocol.Xrm.Query.PagingInvalid",
            description: message);
}
