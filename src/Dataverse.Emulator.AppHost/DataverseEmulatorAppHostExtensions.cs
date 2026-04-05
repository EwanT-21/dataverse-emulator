using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Projects;

namespace Dataverse.Emulator.AppHost;

public static class DataverseEmulatorAppHostExtensions
{
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
}

public sealed record DataverseEmulatorAppHostResource(
    IResourceBuilder<ProjectResource> Service,
    IResourceBuilder<ConnectionStringResource> ConnectionString)
{
    public const string DefaultResourceName = "dataverse-emulator";
    public const string DefaultConnectionStringName = "dataverse";
}
