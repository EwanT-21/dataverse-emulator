using CoreWCF;
using CoreWCF.Configuration;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;
using Dataverse.Emulator.Protocols.Xrm.Requests.Crud;
using Dataverse.Emulator.Protocols.Xrm.Requests.Execution;
using Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Requests.Queries;
using Dataverse.Emulator.Protocols.Xrm.Runtime;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dataverse.Emulator.Protocols.Xrm;

public static class DataverseXrmProtocolExtensions
{
    public static IServiceCollection AddDataverseEmulatorXrmProtocol(this IServiceCollection services)
    {
        services.TryAddSingleton(new DataverseXrmCompatibilitySettings(
            DataverseXrmCompatibilitySettings.DefaultOrganizationVersion,
            DataverseXrmCompatibilitySettings.DefaultOrganizationId,
            DataverseXrmCompatibilitySettings.DefaultOrganizationFriendlyName,
            DataverseXrmCompatibilitySettings.DefaultOrganizationUniqueName,
            DataverseXrmCompatibilitySettings.DefaultOrganizationUserId,
            DataverseXrmCompatibilitySettings.DefaultOrganizationBusinessUnitId,
            DataverseXrmCompatibilitySettings.DefaultProvisionedLanguages.ToArray(),
            DataverseXrmCompatibilitySettings.DefaultInstalledLanguagePacks.ToArray(),
            DataverseXrmCompatibilitySettings.DefaultOrganizationTypeName,
            DataverseXrmCompatibilitySettings.DefaultSolutionUniqueNames.ToArray()));
        services.TryAddSingleton(new DataverseXrmTraceOptions(DataverseXrmTraceOptions.DefaultTraceLimit));
        services.AddHttpContextAccessor();
        services.AddServiceModelServices();
        services.AddSingleton<DataverseXrmCompatibilityProfileService>();
        services.AddSingleton<DataverseXrmRequestTraceStore>();
        services.AddScoped<DataverseXrmRecordOperations>();
        services.AddScoped<DataverseXrmRelationshipOperations>();
        services.AddScoped<DataverseXrmMetadataOperations>();
        services.AddScoped<DataverseXrmRuntimeOperations>();
        services.AddScoped<DataverseXrmOrganizationRequestDispatcher>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveCurrentOrganizationXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, WhoAmIXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, CreateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, UpsertXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, UpdateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, DeleteXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveMultipleXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveVersionXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveAvailableLanguagesXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveDeprovisionedLanguagesXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveProvisionedLanguagesXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveInstalledLanguagePackVersionXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveProvisionedLanguagePackVersionXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveInstalledLanguagePacksXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveOrganizationInfoXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, ExecuteMultipleXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, AssociateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, DisassociateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveEntityMetadataXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveAttributeXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveAllEntitiesXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveRelationshipXrmRequestHandler>();
        services.AddScoped<DataverseOrganizationService>();

        return services;
    }

    public static WebApplication MapDataverseXrm(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (await DataverseXrmMetadataResponder.TryHandleAsync(context))
            {
                return;
            }

            await next();
        });

        app.UseServiceModel(serviceBuilder =>
        {
            serviceBuilder.AddService<DataverseOrganizationService>();
            serviceBuilder.AddServiceEndpoint<DataverseOrganizationService, IOrganizationServiceSoap>(
                new BasicHttpBinding(),
                DataverseXrmConstants.OrganizationServicePath);
        });

        return app;
    }
}
