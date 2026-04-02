using Dataverse.Emulator.Domain.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataverseEmulatorApplication(this IServiceCollection services)
    {
        services.AddSingleton<QueryValidationService>();
        services.AddSingleton<RecordValidationService>();

        services.AddTransient<IValidator<Metadata.GetTableDefinitionByEntitySetNameQuery>, Metadata.GetTableDefinitionByEntitySetNameQueryValidator>();
        services.AddTransient<IValidator<Metadata.GetTableDefinitionQuery>, Metadata.GetTableDefinitionQueryValidator>();
        services.AddTransient<IValidator<Records.CreateRowCommand>, Records.CreateRowCommandValidator>();
        services.AddTransient<IValidator<Records.DeleteRowCommand>, Records.DeleteRowCommandValidator>();
        services.AddTransient<IValidator<Records.GetRowByIdQuery>, Records.GetRowByIdQueryValidator>();
        services.AddTransient<IValidator<Records.ListRowsQuery>, Records.ListRowsQueryValidator>();
        services.AddTransient<IValidator<Records.UpdateRowCommand>, Records.UpdateRowCommandValidator>();

        services.AddTransient<Seeding.SeedScenarioExecutor>();

        return services;
    }
}
