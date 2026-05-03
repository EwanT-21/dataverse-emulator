using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Protocols.Xrm;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.IntegrationTests;

internal sealed class XrmProtocolTestContext : IAsyncDisposable
{
    private readonly ServiceProvider rootProvider;
    private readonly AsyncServiceScope scope;

    private XrmProtocolTestContext(ServiceProvider rootProvider, AsyncServiceScope scope)
    {
        this.rootProvider = rootProvider;
        this.scope = scope;
    }

    public DataverseOrganizationService OrganizationService
        => scope.ServiceProvider.GetRequiredService<DataverseOrganizationService>();

    public DataverseXrmMetadataOperations MetadataOperations
        => scope.ServiceProvider.GetRequiredService<DataverseXrmMetadataOperations>();

    public DataverseXrmOrganizationRequestDispatcher RequestDispatcher
        => scope.ServiceProvider.GetRequiredService<DataverseXrmOrganizationRequestDispatcher>();

    public DataverseXrmRecordOperations RecordOperations
        => scope.ServiceProvider.GetRequiredService<DataverseXrmRecordOperations>();

    public static async Task<XrmProtocolTestContext> CreateAsync(
        SeedScenario scenario,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddDataverseEmulatorApplication();
        services.AddDataverseEmulatorInMemoryPersistence();
        services.AddDataverseEmulatorXrmProtocol();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });
        configureServices?.Invoke(services);

        var rootProvider = services.BuildServiceProvider();

        try
        {
            await using var seedScope = rootProvider.CreateAsyncScope();
            var seedExecutor = seedScope.ServiceProvider.GetRequiredService<SeedScenarioExecutor>();
            await seedExecutor.ExecuteAsync(scenario);

            return new XrmProtocolTestContext(rootProvider, rootProvider.CreateAsyncScope());
        }
        catch
        {
            await rootProvider.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await rootProvider.DisposeAsync();
    }
}
