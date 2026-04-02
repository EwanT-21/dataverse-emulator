using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Dataverse_Emulator_Host>("dataverse-emulator")
    .WithHttpHealthCheck("/status", endpointName: "http");

builder.Build().Run();
