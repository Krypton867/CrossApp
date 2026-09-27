using System.Runtime.InteropServices;
namespace Core;

public static class EnvironmentInfo
{
#if NET10_0_OR_GREATER
    public const string BuildNote = "збірка під net10.0";
#else
    public const string BuildNote = "збірка під net8.0";
#endif

    public static EnvironmentReport Collect()
    {
        return new EnvironmentReport(
            RuntimeInformation.OSDescription,
            Environment.OSVersion.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            AppContext.BaseDirectory,
            BuildNote,
            Environment.CurrentDirectory);
    }
}