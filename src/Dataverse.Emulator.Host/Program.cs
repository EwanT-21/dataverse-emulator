using Dataverse.Emulator.Host.Composition;
using Dataverse.Emulator.Host.Endpoints;
using Dataverse.Emulator.Protocols.WebApi;
using Dataverse.Emulator.Protocols.Xrm;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDataverseEmulatorHost(builder.Configuration);

var app = builder.Build();

app.MapDataverseEmulatorDescriptors();
app.MapDataverseEmulatorBaseline();
app.MapDataverseEmulatorTracing();
app.MapDataverseWebApi();
app.MapDataverseXrm();
app.MapDefaultEndpoints();
app.Run();
