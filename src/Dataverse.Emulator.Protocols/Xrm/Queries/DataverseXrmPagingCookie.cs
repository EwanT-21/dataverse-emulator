using System.Xml.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Dataverse.Emulator.Protocols.Xrm.Queries;

internal static partial class DataverseXrmPagingCookie
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

        var normalizedCookie = DecodeRepeatedly(pagingCookie);

        try
        {
            var cookie = XElement.Parse(normalizedCookie);
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

        var match = ContinuationAttributePattern().Match(normalizedCookie);
        if (match.Success)
        {
            return match.Groups["continuation"].Value;
        }

        return normalizedCookie;
    }

    private static string DecodeRepeatedly(string pagingCookie)
    {
        var current = pagingCookie;

        for (var i = 0; i < 3; i++)
        {
            var decoded = WebUtility.HtmlDecode(current);
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                return current;
            }

            current = decoded;
        }

        return current;
    }

    [GeneratedRegex("""continuation\s*=\s*['"](?<continuation>[^'"]+)['"]""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContinuationAttributePattern();
}
