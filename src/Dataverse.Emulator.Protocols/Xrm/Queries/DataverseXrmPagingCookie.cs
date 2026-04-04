using System.Xml.Linq;

namespace Dataverse.Emulator.Protocols.Xrm.Queries;

internal static class DataverseXrmPagingCookie
{
    public static string Create(string continuationToken, int nextPageNumber)
        => new XElement(
            "cookie",
            new XAttribute("page", nextPageNumber),
            new XAttribute("continuation", continuationToken))
            .ToString(SaveOptions.DisableFormatting);

    public static string? ExtractContinuationToken(string? pagingCookie)
    {
        if (string.IsNullOrWhiteSpace(pagingCookie))
        {
            return null;
        }

        try
        {
            var cookie = XElement.Parse(pagingCookie);
            var continuationToken = cookie.Attribute("continuation")?.Value;
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                return continuationToken;
            }
        }
        catch
        {
            // Fall back to raw continuation tokens to keep the emulator permissive.
        }

        return pagingCookie;
    }
}
