using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Projects;

namespace Dataverse.Emulator.AppHost;

public static class DataverseEmulatorAppHostExtensions
{
    private const string SeedScenarioEnvironmentVariableName = "DATAVERSE_EMULATOR_SEED_SCENARIO";
    private const string SnapshotPathEnvironmentVariableName = "DATAVERSE_EMULATOR_SNAPSHOT_PATH";
    private const string OrganizationVersionEnvironmentVariableName = "DATAVERSE_EMULATOR_ORGANIZATION_VERSION";
    private const string XrmTraceLimitEnvironmentVariableName = "DATAVERSE_EMULATOR_XRM_TRACE_LIMIT";
    private const string TelemetryEnabledEnvironmentVariableName = "DATAVERSE_EMULATOR_TELEMETRY_ENABLED";
    private const string TelemetryEndpointEnvironmentVariableName = "DATAVERSE_EMULATOR_TELEMETRY_ENDPOINT";

    public static DataverseEmulatorAppHostResource AddDataverseEmulator(
        this IDistributedApplicationBuilder builder,
        string resourceName = DataverseEmulatorAppHostResource.DefaultResourceName,
        string connectionStringName = DataverseEmulatorAppHostResource.DefaultConnectionStringName)
    {
        var emulator = builder.AddProject<Dataverse_Emulator_Host>(resourceName)
            .WithHttpHealthCheck("/status", endpointName: "http");

        var httpEndpoint = emulator.Resource.GetEndpoint("http");
        var connectionString = builder.AddConnectionString(connectionStringName, expression =>
        {
            expression.AppendLiteral("AuthType=AD;Url=");
            expression.AppendFormatted(httpEndpoint.Property(EndpointProperty.Url));
            expression.AppendLiteral("/org;Domain=EMULATOR;Username=local;Password=local");
        });

        return new DataverseEmulatorAppHostResource(emulator, connectionString);
    }

    public static IResourceBuilder<TDestination> WithDataverseConnectionString<TDestination>(
        this IResourceBuilder<TDestination> destination,
        DataverseEmulatorAppHostResource resource,
        string environmentVariableName)
        where TDestination : IResourceWithEnvironment, IResourceWithWaitSupport
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentVariableName);

        destination.WithEnvironment(environmentVariableName, resource.ConnectionString);
        destination.WaitFor(resource.Service);

        return destination;
    }

    public static DataverseEmulatorAppHostResource WithSeedScenario(
        this DataverseEmulatorAppHostResource resource,
        string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        resource.Service.WithEnvironment(
            SeedScenarioEnvironmentVariableName,
            scenarioName);

        return resource;
    }

    public static DataverseEmulatorAppHostResource WithSnapshotFile(
        this DataverseEmulatorAppHostResource resource,
        string snapshotPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);

        resource.Service.WithEnvironment(
            SnapshotPathEnvironmentVariableName,
            snapshotPath);

        return resource;
    }

    public static DataverseEmulatorAppHostResource WithOrganizationVersion(
        this DataverseEmulatorAppHostResource resource,
        string organizationVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationVersion);

        resource.Service.WithEnvironment(
            OrganizationVersionEnvironmentVariableName,
            organizationVersion);

        return resource;
    }

    public static DataverseEmulatorAppHostResource WithXrmTraceLimit(
        this DataverseEmulatorAppHostResource resource,
        int traceLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(traceLimit);

        resource.Service.WithEnvironment(
            XrmTraceLimitEnvironmentVariableName,
            traceLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));

        return resource;
    }

    public static DataverseEmulatorAppHostResource WithCompatibilityTelemetryEndpoint(
        this DataverseEmulatorAppHostResource resource,
        string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Compatibility telemetry endpoint must be an absolute URI.", nameof(endpoint));
        }

        resource.Service.WithEnvironment(
            TelemetryEndpointEnvironmentVariableName,
            endpoint);

        return resource;
    }

    public static DataverseEmulatorAppHostResource WithoutCompatibilityTelemetry(
        this DataverseEmulatorAppHostResource resource)
    {
        resource.Service.WithEnvironment(
            TelemetryEnabledEnvironmentVariableName,
            bool.FalseString.ToLowerInvariant());

        return resource;
    }
}

public sealed record DataverseEmulatorAppHostResource(
    IResourceBuilder<ProjectResource> Service,
    IResourceBuilder<ConnectionStringResource> ConnectionString)
{
    public const string DefaultResourceName = "dataverse-emulator";
    public const string DefaultConnectionStringName = "dataverse";
}
