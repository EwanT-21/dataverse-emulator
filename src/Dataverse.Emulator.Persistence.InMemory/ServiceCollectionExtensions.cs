using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Persistence.InMemory.Metadata;
using Dataverse.Emulator.Persistence.InMemory.Records;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Persistence.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataverseEmulatorInMemoryPersistence(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMetadataRepository>();
        services.AddSingleton<IReadRepository<TableDefinition>>(sp => sp.GetRequiredService<InMemoryMetadataRepository>());
        services.AddSingleton<IRepository<TableDefinition>>(sp => sp.GetRequiredService<InMemoryMetadataRepository>());

        services.AddSingleton<InMemoryRecordRepository>();
        services.AddSingleton<IReadRepository<EntityRecord>>(sp => sp.GetRequiredService<InMemoryRecordRepository>());
        services.AddSingleton<IRepository<EntityRecord>>(sp => sp.GetRequiredService<InMemoryRecordRepository>());
        services.AddSingleton<IRecordQueryService>(sp => sp.GetRequiredService<InMemoryRecordRepository>());

        return services;
    }
}
