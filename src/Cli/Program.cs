using Core;
using System.Text.Json;

EnvironmentReport report = EnvironmentInfo.Collect();

string domain = "Склад (товари, партії, залишки, переміщення)";

if (args.Contains("--json"))
{
    var information = new
    {
        report.OSDescription,
        report.OSEnvironment,
        report.Architecture,
        report.DotnetVersion,
        report.Runtime,
        report.ApplicationDirectory,
        report.CurrentDirectory,
        domain
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Console.WriteLine(JsonSerializer.Serialize(information, options));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Володимир Врублевський, група FEI-33");
    Console.WriteLine(new string('-', 70));

    Console.WriteLine($"{"ОС:",-25} {report.OSDescription}");
    Console.WriteLine($"{"ОС Environment:",-25} {report.OSEnvironment}");
    Console.WriteLine($"{"Архітектура:",-25} {report.Architecture}");
    Console.WriteLine($"{"Версія .NET:",-25} {report.DotnetVersion}");
    Console.WriteLine($"{"Runtime:",-25} {report.Runtime}");
    Console.WriteLine($"{"Каталог застосунку:",-25} {report.ApplicationDirectory}");
    Console.WriteLine($"{"Поточний каталог:",-25} {report.CurrentDirectory}");
    Console.WriteLine($"{"Предметна область:",-25} {domain}");

    Console.WriteLine(new string('-', 70));
}