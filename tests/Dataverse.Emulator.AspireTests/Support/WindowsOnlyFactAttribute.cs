namespace Dataverse.Emulator.AspireTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class WindowsOnlyFactAttribute : FactAttribute
{
    public WindowsOnlyFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires Windows to launch the .NET Framework 4.8 CrmServiceClient harness.";
        }
    }
}
