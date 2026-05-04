using Dataverse.Emulator.Protocols.Common.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Host.Telemetry;

public static class DataverseCompatibilityTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddDataverseCompatibilityTelemetry(
        this IServiceCollection services,
        DataverseCompatibilityTelemetryOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsActive)
        {
            return services;
        }

        services.AddSingleton(options);
        services.AddHttpClient(DataverseCompatibilityTelemetryHttpClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<DataverseCompatibilityTelemetryHttpClient>();
        services.AddSingleton<DataverseCompatibilityTelemetryDispatcher>();
        services.AddSingleton<IDataverseCompatibilityTelemetry>(serviceProvider =>
            serviceProvider.GetRequiredService<DataverseCompatibilityTelemetryDispatcher>());
        services.AddHostedService(serviceProvider =>
            serviceProvider.GetRequiredService<DataverseCompatibilityTelemetryDispatcher>());

        return services;
    }
}
