using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Host;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Protocols.WebApi;
using Dataverse.Emulator.Protocols.Xrm;
using Mediator;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDataverseEmulatorApplication();
builder.Services.AddDataverseEmulatorInMemoryPersistence();
builder.Services.AddDataverseEmulatorXrmProtocol();
builder.Services.AddHostedService<DefaultSeedHostedService>();
builder.Services.AddMediator(options =>
{
    options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
    options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
});

var app = builder.Build();

app.MapGet(
    "/",
    () => Results.Ok(
        new EmulatorDescriptor(
            "Dataverse Emulator",
            "Local emulator slice implemented for account metadata, Xrm/C# CRUD, QueryExpression, and matching Web API CRUD",
            [
                "Xrm/C# organization service",
                "Dataverse Web API"
            ],
            "In-memory persistence",
            "AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local")));

app.MapGet("/status", () => Results.Ok(new HealthDescriptor("healthy", DateTimeOffset.UtcNow)));

app.MapPost(
    "/_emulator/v1/reset",
    async (SeedScenarioExecutor seedScenarioExecutor, CancellationToken cancellationToken) =>
    {
        await seedScenarioExecutor.ExecuteAsync(DefaultSeedScenarioFactory.Create(), cancellationToken);
        return Results.Ok(new EmulatorResetDescriptor(
            "reset",
            "default-seed",
            DateTimeOffset.UtcNow));
    });

app.MapGet(
    "/roadmap",
    () => Results.Ok(
        new[]
        {
            "Primary compatibility target: hosted CrmServiceClient bootstrap against /org.",
            "Current table slice: account metadata plus CRUD and RetrieveMultiple(QueryExpression).",
            "Secondary compatibility surface: matching Dataverse Web API CRUD on /api/data/v9.2/accounts.",
            "Local workflow support: reset the emulator to its default seeded state through /_emulator/v1/reset."
        }));

app.MapDataverseWebApi();
app.MapDataverseXrm();
app.MapDefaultEndpoints();
app.Run();

public sealed record EmulatorDescriptor(
    string Name,
    string Status,
    string[] Protocols,
    string Persistence,
    string ConnectionStringTemplate);

public sealed record HealthDescriptor(string Status, DateTimeOffset UtcNow);

public sealed record EmulatorResetDescriptor(
    string Status,
    string Scenario,
    DateTimeOffset UtcNow);

public partial class Program
{
}
