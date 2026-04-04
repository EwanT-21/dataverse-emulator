using Microsoft.AspNetCore.Http;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmMetadataResponder
{
    public static async Task<bool> TryHandleAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method)
            || !context.Request.Path.Equals(DataverseXrmConstants.OrganizationServicePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (context.Request.Query.TryGetValue("wsdl", out var wsdlValues))
        {
            var documentName = string.IsNullOrWhiteSpace(wsdlValues.ToString())
                ? "wsdl1.xml"
                : $"{wsdlValues}.xml";

            var document = await ReadTemplateAsync(documentName);
            if (document is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return true;
            }

            var serviceUri = BuildServiceUri(context);
            var content = string.IsNullOrWhiteSpace(wsdlValues.ToString())
                ? RewriteRootWsdl(document, serviceUri)
                : RewriteContractWsdl(document, serviceUri);

            await WriteXmlAsync(context, content);
            return true;
        }

        if (context.Request.Query.TryGetValue("xsd", out var xsdValues))
        {
            var documentName = xsdValues.ToString();
            if (!documentName.StartsWith("xsd", StringComparison.OrdinalIgnoreCase)
                || documentName.Length <= 3
                || !char.IsDigit(documentName[^1]))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return true;
            }

            var document = await ReadTemplateAsync($"{documentName}.xsd");
            if (document is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return true;
            }

            await WriteXmlAsync(context, document);
            return true;
        }

        return false;
    }

    private static async Task<string?> ReadTemplateAsync(string fileName)
    {
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            DataverseXrmConstants.MetadataRootFolder,
            DataverseXrmConstants.MetadataProfileFolder,
            fileName);

        return File.Exists(filePath)
            ? await File.ReadAllTextAsync(filePath)
            : null;
    }

    private static string RewriteRootWsdl(string content, string serviceUri)
    {
        content = content.Replace(
            "location=\"\"",
            $"location=\"{serviceUri}?wsdl=wsdl0\"",
            StringComparison.Ordinal);

        return content.Replace(
            "location=\"http://localhost:5022/org/XRMServices/2011/Organization.svc\"",
            $"location=\"{serviceUri}\"",
            StringComparison.Ordinal);
    }

    private static string RewriteContractWsdl(string content, string serviceUri)
    {
        for (var index = 0; index <= 20; index++)
        {
            var originalImport = index == 5
                ? "<xsd:import />"
                : $"<xsd:import namespace=\"{DataverseXrmMetadataNamespaces.GetNamespace(index)}\" />";
            var rewrittenImport = index == 5
                ? $"<xsd:import schemaLocation=\"{serviceUri}?xsd=xsd5\" />"
                : $"<xsd:import namespace=\"{DataverseXrmMetadataNamespaces.GetNamespace(index)}\" schemaLocation=\"{serviceUri}?xsd=xsd{index}\" />";

            content = content.Replace(originalImport, rewrittenImport, StringComparison.Ordinal);
        }

        return content.Replace(
            "http://localhost:5022/org/XRMServices/2011/Organization.svc",
            serviceUri,
            StringComparison.Ordinal);
    }

    private static async Task WriteXmlAsync(HttpContext context, string content)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/xml; charset=utf-8";
        await context.Response.WriteAsync(content);
    }

    private static string BuildServiceUri(HttpContext context)
        => $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{DataverseXrmConstants.OrganizationServicePath}";
}

internal static class DataverseXrmMetadataNamespaces
{
    public static string GetNamespace(int index) => index switch
    {
        0 => "http://schemas.microsoft.com/xrm/2011/Contracts/Services",
        1 => "http://schemas.microsoft.com/2003/10/Serialization/",
        2 => "http://schemas.microsoft.com/xrm/2011/Contracts",
        3 => "http://schemas.datacontract.org/2004/07/System.Collections.Generic",
        4 => "http://schemas.microsoft.com/2003/10/Serialization/Arrays",
        6 => "http://schemas.microsoft.com/xrm/7.1/Contracts",
        7 => "http://schemas.microsoft.com/xrm/2011/Metadata",
        8 => "http://schemas.microsoft.com/xrm/2013/Metadata",
        9 => "http://schemas.microsoft.com/xrm/7.1/Metadata",
        10 => "http://schemas.microsoft.com/xrm/9.0/Metadata",
        11 => "http://schemas.microsoft.com/xrm/2014/Contracts",
        12 => "http://schemas.microsoft.com/xrm/9.0/Contracts",
        13 => "http://schemas.microsoft.com/xrm/8.1/Contracts",
        14 => "http://schemas.microsoft.com/xrm/2011/Metadata/Query",
        15 => "http://schemas.microsoft.com/xrm/8.0",
        16 => "http://schemas.microsoft.com/xrm/2012/Contracts",
        17 => "http://schemas.datacontract.org/2004/07/Microsoft.Xrm.Sdk",
        18 => "http://schemas.microsoft.com/xrm/8.2/Contracts",
        19 => "http://schemas.datacontract.org/2004/07/System",
        20 => "http://schemas.datacontract.org/2004/07/Microsoft.Xrm.Sdk.Metadata",
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
