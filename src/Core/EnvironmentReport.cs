namespace Core;

public sealed record EnvironmentReport(
    string OSDescription,
    string OSEnvironment,
    string Architecture,
    string DotnetVersion,
    string Runtime,
    string ApplicationDirectory,
    string BuildNote,
    string CurrentDirectory);