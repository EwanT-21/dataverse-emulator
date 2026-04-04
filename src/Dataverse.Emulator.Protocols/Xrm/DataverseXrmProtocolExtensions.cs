using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.Protocols.Xrm;

public static class DataverseXrmProtocolExtensions
{
    public static IServiceCollection AddDataverseEmulatorXrmProtocol(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddServiceModelServices();
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
