using CoreWCF;
using CoreWCF.Configuration;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Dataverse.Emulator.Protocols.Xrm.Requests.Bootstrap;
using Dataverse.Emulator.Protocols.Xrm.Requests.Crud;
using Dataverse.Emulator.Protocols.Xrm.Requests.Execution;
using Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Requests.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Protocols.Xrm;

public static class DataverseXrmProtocolExtensions
{
    public static IServiceCollection AddDataverseEmulatorXrmProtocol(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddServiceModelServices();
        services.AddScoped<DataverseXrmRecordOperations>();
        services.AddScoped<DataverseXrmMetadataOperations>();
        services.AddScoped<DataverseXrmOrganizationRequestDispatcher>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveCurrentOrganizationXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, WhoAmIXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, CreateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, UpdateXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, DeleteXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveMultipleXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, ExecuteMultipleXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveEntityMetadataXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveAttributeXrmRequestHandler>();
        services.AddScoped<IXrmOrganizationRequestHandler, RetrieveAllEntitiesXrmRequestHandler>();
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
