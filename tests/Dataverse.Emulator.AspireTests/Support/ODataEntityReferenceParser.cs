namespace Dataverse.Emulator.AspireTests;

internal static class ODataEntityReferenceParser
{
    public static Guid ExtractId(string entityUri)
    {
        var start = entityUri.IndexOf('(');
        var end = entityUri.IndexOf(')', start + 1);
        return Guid.Parse(entityUri[(start + 1)..end]);
    }
}
