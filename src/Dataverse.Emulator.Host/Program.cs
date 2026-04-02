var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet(
    "/",
    () => Results.Ok(
        new EmulatorDescriptor(
            "Dataverse Emulator",
            "Scaffold",
            [
                "Dataverse Web API",
                "XRM-compatible surface"
            ],
            "In-memory persistence")));

app.MapGet("/health", () => Results.Ok(new HealthDescriptor("healthy", DateTimeOffset.UtcNow)));

app.MapGet(
    "/roadmap",
    () => Results.Ok(
        new[]
        {
            "Model metadata, records, and relationships in the domain.",
            "Implement CRUD and query orchestration in the application layer.",
            "Expose validated compatibility slices through protocol adapters."
        }));

app.Run();

public sealed record EmulatorDescriptor(
    string Name,
    string Status,
    string[] Protocols,
    string Persistence);

public sealed record HealthDescriptor(string Status, DateTimeOffset UtcNow);

public partial class Program
{
}
