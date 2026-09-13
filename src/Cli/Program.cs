using System.Runtime.InteropServices;
using System.Text.Json;

string osDescription = RuntimeInformation.OSDescription;
string osEnvironment = Environment.OSVersion.ToString();
string architecture = RuntimeInformation.ProcessArchitecture.ToString();
string dotnetVersion = Environment.Version.ToString();
string runtime = RuntimeInformation.FrameworkDescription;
string applicationDirectory = AppContext.BaseDirectory;
string currentDirectory = Environment.CurrentDirectory;
string domain = "Склад (товари, партії, залишки, переміщення)";

if (args.Contains("--json"))
{
    var information = new
    {
        osDescription,
        osEnvironment,
        architecture,
        dotnetVersion,
        runtime,
        applicationDirectory,
        currentDirectory,
        domain
    };

    var options = new JsonSerializerOptions
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    Console.WriteLine(JsonSerializer.Serialize(information, options));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Володимир Врублевський, група FEI-33");
    Console.WriteLine(new string('-', 70));

    Console.WriteLine($"{"ОС:",-25} {osDescription}");
    Console.WriteLine($"{"ОС Environment:",-25} {osEnvironment}");
    Console.WriteLine($"{"Архітектура:",-25} {architecture}");
    Console.WriteLine($"{"Версія .NET:",-25} {dotnetVersion}");
    Console.WriteLine($"{"Runtime:",-25} {runtime}");
    Console.WriteLine($"{"Каталог застосунку:",-25} {applicationDirectory}");
    Console.WriteLine($"{"Поточний каталог:",-25} {currentDirectory}");
    Console.WriteLine($"{"Предметна область:",-25} {domain}");

    Console.WriteLine(new string('-', 70));
}