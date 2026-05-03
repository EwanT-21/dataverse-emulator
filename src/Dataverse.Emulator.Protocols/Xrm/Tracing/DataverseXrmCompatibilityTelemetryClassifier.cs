using System.Reflection;
using Dataverse.Emulator.Protocols.Common.Telemetry;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Tracing;

public sealed class DataverseXrmCompatibilityTelemetryClassifier
{
    private const string UnsupportedCapabilityEventKind = "unsupported-capability";
    private const string ProtocolName = "xrm";
    private const string CustomOrUnknownCapabilityKey = "custom-or-unknown";
    private static readonly IReadOnlySet<string> KnownSdkRequestNames = CreateKnownSdkRequestNames();
    private static readonly IReadOnlySet<string> KnownQueryTypeNames = CreateAssignableTypeNames(typeof(QueryBase));
    private static readonly IReadOnlySet<string> KnownEntityMetadataPropertyNames = CreatePropertyNames(typeof(EntityMetadata));
    private static readonly IReadOnlySet<string> KnownAttributeMetadataPropertyNames = CreatePropertyNames(typeof(AttributeMetadata));
    private static readonly IReadOnlySet<string> KnownRelationshipMetadataPropertyNames = CreatePropertyNames(typeof(OneToManyRelationshipMetadata));
    private static readonly IReadOnlySet<string> KnownMetadataConditionOperatorNames = Enum
        .GetNames<MetadataConditionOperator>()
        .ToHashSet(StringComparer.Ordinal);

    public DataverseCompatibilityTelemetryEvent? Classify(
        string source,
        string name,
        OrganizationServiceFault fault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fault);

        if (!fault.ErrorDetails.TryGetValue("DataverseEmulator.ErrorCode", out var errorCodeValue)
            || errorCodeValue is not string errorCode)
        {
            return null;
        }

        return errorCode switch
        {
            "Protocol.Xrm.Execute.Unsupported" => CreateEvent(
                source,
                errorCode,
                "organization-request",
                SanitizeRequestName(name)),
            "Protocol.Xrm.Operation.Unsupported" => ClassifyUnsupportedOperation(source, errorCode, fault.Message),
            "Protocol.Xrm.Query.Unsupported" => ClassifyWrappedValue(
                source,
                errorCode,
                fault.Message,
                "QueryExpression feature '",
                "' is not supported by the local Dataverse emulator.",
                "query-feature"),
            "Protocol.Xrm.Query.UnsupportedType" => ClassifyWrappedValue(
                source,
                errorCode,
                fault.Message,
                "Query type '",
                "' is not supported by the local Dataverse emulator.",
                "query-type",
                value => SanitizeIdentifier(value, KnownQueryTypeNames)),
            "Protocol.Xrm.FetchXml.Unsupported" => ClassifyWrappedValue(
                source,
                errorCode,
                fault.Message,
                "FetchXML feature '",
                "' is not supported by the local Dataverse emulator.",
                "fetchxml-feature"),
            _ => null
        };
    }

    private DataverseCompatibilityTelemetryEvent? ClassifyUnsupportedOperation(
        string source,
        string errorCode,
        string? message)
    {
        var operation = TryExtractWrappedValue(
            message,
            "Operation '",
            "' is not supported by the local Dataverse emulator.");
        if (string.IsNullOrWhiteSpace(operation))
        {
            return CreateEvent(source, errorCode, "operation", CustomOrUnknownCapabilityKey);
        }

        if (TryMatchWrappedValue(
                operation,
                "RetrieveMetadataChanges metadata property '",
                "'",
                out var entityProperty))
        {
            return CreateEvent(
                source,
                errorCode,
                "metadata-entity-property",
                SanitizeIdentifier(entityProperty, KnownEntityMetadataPropertyNames));
        }

        if (TryMatchWrappedValue(
                operation,
                "RetrieveMetadataChanges attribute metadata property '",
                "'",
                out var attributeProperty))
        {
            return CreateEvent(
                source,
                errorCode,
                "metadata-attribute-property",
                SanitizeIdentifier(attributeProperty, KnownAttributeMetadataPropertyNames));
        }

        if (TryMatchWrappedValue(
                operation,
                "RetrieveMetadataChanges relationship metadata property '",
                "'",
                out var relationshipProperty))
        {
            return CreateEvent(
                source,
                errorCode,
                "metadata-relationship-property",
                SanitizeIdentifier(relationshipProperty, KnownRelationshipMetadataPropertyNames));
        }

        if (TryMatchWrappedValue(
                operation,
                "RetrieveMetadataChanges metadata condition operator '",
                "'",
                out var conditionOperator))
        {
            return CreateEvent(
                source,
                errorCode,
                "metadata-condition-operator",
                SanitizeIdentifier(conditionOperator, KnownMetadataConditionOperatorNames));
        }

        if (TryMatchWrappedValue(
                operation,
                "ExecuteTransaction child request '",
                "'",
                out var childRequestName))
        {
            return CreateEvent(
                source,
                errorCode,
                "nested-organization-request",
                SanitizeRequestName(childRequestName));
        }

        return operation switch
        {
            "Upsert alternate keys" => CreateEvent(source, errorCode, "operation", "UpsertAlternateKeys"),
            "RetrieveAttribute by ColumnNumber" => CreateEvent(source, errorCode, "operation", "RetrieveAttributeByColumnNumber"),
            "Retrieve related entities" => CreateEvent(source, errorCode, "operation", "RetrieveRelatedEntities"),
            "Related entity graphs" => CreateEvent(source, errorCode, "operation", "RelatedEntityGraphs"),
            _ => CreateEvent(source, errorCode, "operation", CustomOrUnknownCapabilityKey)
        };
    }

    private DataverseCompatibilityTelemetryEvent? ClassifyWrappedValue(
        string source,
        string errorCode,
        string? message,
        string prefix,
        string suffix,
        string capabilityKind,
        Func<string, string>? sanitizer = null)
    {
        var value = TryExtractWrappedValue(message, prefix, suffix);
        if (string.IsNullOrWhiteSpace(value))
        {
            return CreateEvent(source, errorCode, capabilityKind, CustomOrUnknownCapabilityKey);
        }

        return CreateEvent(
            source,
            errorCode,
            capabilityKind,
            sanitizer is null ? value : sanitizer(value));
    }

    private static DataverseCompatibilityTelemetryEvent CreateEvent(
        string source,
        string errorCode,
        string capabilityKind,
        string capabilityKey)
        => new(
            UnsupportedCapabilityEventKind,
            ProtocolName,
            source,
            errorCode,
            capabilityKind,
            capabilityKey,
            DateTimeOffset.UtcNow);

    private static string SanitizeRequestName(string requestName)
        => SanitizeIdentifier(requestName, KnownSdkRequestNames);

    private static string SanitizeIdentifier(
        string value,
        IReadOnlySet<string> knownValues)
        => knownValues.Contains(value)
            ? value
            : CustomOrUnknownCapabilityKey;

    private static bool TryMatchWrappedValue(
        string input,
        string prefix,
        string suffix,
        out string value)
    {
        value = TryExtractWrappedValue(input, prefix, suffix) ?? string.Empty;
        return value.Length > 0;
    }

    private static string? TryExtractWrappedValue(
        string? value,
        string prefix,
        string suffix)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !value.StartsWith(prefix, StringComparison.Ordinal)
            || !value.EndsWith(suffix, StringComparison.Ordinal))
        {
            return null;
        }

        return value[prefix.Length..^suffix.Length];
    }

    private static IReadOnlySet<string> CreateKnownSdkRequestNames()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly =>
                string.Equals(assembly.GetName().Name, "Microsoft.Xrm.Sdk", StringComparison.Ordinal)
                || string.Equals(assembly.GetName().Name, "Microsoft.Crm.Sdk.Proxy", StringComparison.Ordinal))
            .ToArray();

        var knownNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "WhoAmI",
            "RetrieveCurrentOrganization",
            "RetrieveVersion",
            "RetrieveAvailableLanguages",
            "RetrieveDeprovisionedLanguages",
            "RetrieveProvisionedLanguages",
            "RetrieveInstalledLanguagePackVersion",
            "RetrieveProvisionedLanguagePackVersion",
            "RetrieveInstalledLanguagePacks",
            "RetrieveOrganizationInfo",
            "ExecuteMultiple",
            "ExecuteTransaction",
            "Associate",
            "Disassociate",
            "RetrieveEntity",
            "RetrieveAttribute",
            "RetrieveAllEntities",
            "RetrieveRelationship",
            "RetrieveMetadataChanges",
            "RetrieveUserLicenseInfo"
        };
        foreach (var assembly in assemblies)
        {
            foreach (var type in GetLoadableTypes(assembly).Where(type =>
                         !type.IsAbstract
                         && typeof(OrganizationRequest).IsAssignableFrom(type)
                         && type.Name.EndsWith("Request", StringComparison.Ordinal)))
            {
                knownNames.Add(type.Name[..^"Request".Length]);
            }
        }

        return knownNames;
    }

    private static IReadOnlySet<string> CreateAssignableTypeNames(Type baseType)
    {
        var knownNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in GetLoadableTypes(assembly).Where(type =>
                         !type.IsAbstract
                         && baseType.IsAssignableFrom(type)))
            {
                knownNames.Add(type.Name);
            }
        }

        return knownNames;
    }

    private static IReadOnlySet<string> CreatePropertyNames(Type type)
        => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes().Where(type => type is not null)!;
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
