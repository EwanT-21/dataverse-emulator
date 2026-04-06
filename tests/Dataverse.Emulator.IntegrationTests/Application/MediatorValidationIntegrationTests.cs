using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Persistence.InMemory;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.IntegrationTests;

public class MediatorValidationIntegrationTests
{
    [Fact]
    public async Task MediatorPipeline_ReturnsValidationErrors_BeforeHandlerExecution()
    {
        var services = new ServiceCollection();
        services.AddDataverseEmulatorApplication();
        services.AddDataverseEmulatorInMemoryPersistence();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CreateRowCommand(
                string.Empty,
                null!));

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code.StartsWith("Validation.", StringComparison.Ordinal));
    }
}
