using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Host.Telemetry;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Protocols.Xrm;
using Dataverse.Emulator.Protocols.Xrm.Runtime;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Host.Composition;

public static class DataverseEmulatorHostServiceRegistration
{
    public static IServiceCollection AddDataverseEmulatorHost(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDataverseEmulatorApplication();
        services.AddDataverseEmulatorInMemoryPersistence();
        services.AddDataverseEmulatorXrmProtocol();

        services.AddSingleton(_ => new DataverseEmulatorBaselineSettings(
            SeedScenarioName: DataverseEmulatorHostSettingsResolver.ResolveSeedScenarioName(
                configuration[DataverseEmulatorHostEnvironmentVariables.SeedScenarioEnvironmentVariableName]),
            SnapshotPath: DataverseEmulatorHostSettingsResolver.ResolveSnapshotPath(
                configuration[DataverseEmulatorHostEnvironmentVariables.SnapshotPathEnvironmentVariableName])));

        services.AddSingleton(_ => new DataverseXrmCompatibilitySettings(
            OrganizationVersion: DataverseEmulatorHostSettingsResolver.ResolveOrganizationVersion(
                configuration[DataverseEmulatorHostEnvironmentVariables.OrganizationVersionEnvironmentVariableName]),
            OrganizationId: DataverseXrmCompatibilitySettings.DefaultOrganizationId,
            OrganizationFriendlyName: DataverseXrmCompatibilitySettings.DefaultOrganizationFriendlyName,
            OrganizationUniqueName: DataverseXrmCompatibilitySettings.DefaultOrganizationUniqueName,
            DefaultUserId: DataverseXrmCompatibilitySettings.DefaultOrganizationUserId,
            DefaultBusinessUnitId: DataverseXrmCompatibilitySettings.DefaultOrganizationBusinessUnitId,
            ProvisionedLanguages: DataverseXrmCompatibilitySettings.DefaultProvisionedLanguages.ToArray(),
            InstalledLanguagePacks: DataverseXrmCompatibilitySettings.DefaultInstalledLanguagePacks.ToArray(),
            OrganizationTypeName: DataverseXrmCompatibilitySettings.DefaultOrganizationTypeName,
            SolutionUniqueNames: DataverseXrmCompatibilitySettings.DefaultSolutionUniqueNames.ToArray()));

        services.AddSingleton(_ => new DataverseXrmTraceOptions(
            DataverseEmulatorHostSettingsResolver.ResolveXrmTraceLimit(
                configuration[DataverseEmulatorHostEnvironmentVariables.XrmTraceLimitEnvironmentVariableName])));

        services.AddDataverseCompatibilityTelemetry(new DataverseCompatibilityTelemetryOptions(
            DataverseCompatibilityTelemetryConfiguration.ResolveEnabled(
                configuration[DataverseEmulatorHostEnvironmentVariables.TelemetryEnabledEnvironmentVariableName]),
            DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint(
                configuration[DataverseEmulatorHostEnvironmentVariables.TelemetryEndpointEnvironmentVariableName])));

        services.AddHostedService<DefaultSeedHostedService>();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        return services;
    }
}
