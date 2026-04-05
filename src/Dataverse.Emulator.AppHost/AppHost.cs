using Dataverse.Emulator.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDataverseEmulator();

builder.Build().Run();
