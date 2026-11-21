using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core;
using Core.Abstractions;
using Core.Configuration;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;
using Microsoft.Extensions.Configuration;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    try
    {
        // Файли appsettings копіюються поряд із застосунком під час build.
        string appDirectory = AppContext.BaseDirectory;
        IConfigurationRoot preliminaryConfig = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .Build();

        string environmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? preliminaryConfig["EnvironmentName"]
            ?? "Production";

        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .Build();

        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();
        options.EnvironmentName = environmentName;

        // Спеціальна проста змінна має найвищий пріоритет для каталогу даних.
        string? simpleDataPath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(simpleDataPath))
            options.Storage.DataPath = simpleDataPath;

        if (string.IsNullOrWhiteSpace(options.Storage.DataPath))
            throw new DomainException("Storage:DataPath не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(options.Storage.FileName) ||
            Path.GetFileName(options.Storage.FileName) != options.Storage.FileName ||
            options.Storage.FileName is "." or "..")
            throw new DomainException("Storage:FileName має бути лише назвою файлу.");

        string dataDirectory = Path.IsPathRooted(options.Storage.DataPath)
            ? Path.GetFullPath(options.Storage.DataPath)
            : Path.GetFullPath(Path.Combine(appDirectory, options.Storage.DataPath));
        string dataFile = Path.Combine(dataDirectory, options.Storage.FileName);

        ICatalogStore store = StoreFactory.Create(args, dataFile);
        var service = new CatalogService(store);

        Console.WriteLine("CrossApp — лабораторна робота 9");
        Console.WriteLine($"Профіль: {options.EnvironmentName}");
        Console.WriteLine($"Рівень логування: {options.Logging.MinimumLevel}");
        Console.WriteLine($"Одиниця за замовчуванням: {options.Warehouse.DefaultUnit}");
        Console.WriteLine($"Сховище: {(args.Contains("--file", StringComparer.OrdinalIgnoreCase) ? "файлове" : "у пам'яті")}");
        Console.WriteLine($"Файл даних: {dataFile}");
        Console.WriteLine($"ApiKey: {(string.IsNullOrWhiteSpace(config["Integration:ApiKey"]) ? "відсутній" : "присутній")}");
        Console.WriteLine();

        // Ігноруємо прапорці і значення параметра --timeout-seconds.
        int commandIndex = -1;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--timeout-seconds", StringComparison.OrdinalIgnoreCase))
            {
                i++;
                continue;
            }
            if (args[i].StartsWith("--", StringComparison.Ordinal))
                continue;
            commandIndex = i;
            break;
        }

        string command = commandIndex < 0 ? "report-top" : args[commandIndex];
        if (command is "import" or "import-stream")
        {
            string sourcePath = dataFile;
            if (commandIndex + 1 < args.Length &&
                !args[commandIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                sourcePath = Path.GetFullPath(args[commandIndex + 1]);
            }

            int timeoutSeconds = 60;
            int timeoutIndex = Array.FindIndex(args, a =>
                a.Equals("--timeout-seconds", StringComparison.OrdinalIgnoreCase));
            if (timeoutIndex >= 0 &&
                (timeoutIndex + 1 >= args.Length ||
                 !int.TryParse(args[timeoutIndex + 1], out timeoutSeconds) ||
                 timeoutSeconds <= 0))
            {
                Console.WriteLine("Тайм-аут повинен бути додатним цілим числом.");
                return 1;
            }

            return await RunImportAsync(service, sourcePath,
                command == "import-stream", timeoutSeconds);
        }

        if (command == "export")
        {
            // Експорт усієї поточної колекції до конфігурованого файлу.
            IReadOnlyList<Product> products = service.All();
            Directory.CreateDirectory(dataDirectory);
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(products.Select(p => p.ToDto()).ToList(), jsonOptions);
            string temporaryPath = dataFile + ".tmp";
            try
            {
                await File.WriteAllTextAsync(temporaryPath, json);
                File.Move(temporaryPath, dataFile, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            Console.WriteLine($"Експортовано {products.Count} записів.");
            return 0;
        }

        IReadOnlyList<Product> items = service.All();
        switch (command)
        {
            case "report-top":
                Console.WriteLine("=== ТОП-5 товарів за залишком ===");
                Print(ReportService.TopByQuantity(items, 5));
                break;
            case "report-low":
                Console.WriteLine("=== Товари з залишком менше 10 ===");
                Print(ReportService.LowStock(items, 10));
                break;
            case "report-groups":
                Console.WriteLine("=== Групування за одиницями вимірювання ===");
                foreach (var group in ReportService.ByUnit(items))
                    Console.WriteLine($"{group.Key,-8} позицій: {group.Count,3} сумарно: {group.TotalQuantity,6}");
                break;
            case "report-sum":
                Console.WriteLine("=== Загальна статистика ===");
                var totals = ReportService.Totals(items);
                Console.WriteLine($"Кількість позицій: {totals.Items}");
                Console.WriteLine($"Загальний залишок: {totals.Total}");
                Console.WriteLine($"Середній залишок:  {totals.Average:F2}");
                Console.WriteLine($"Максимальний залишок: {totals.Max}");
                break;
            case "report-top-units":
                Console.WriteLine("=== ТОП-3 одиниць вимірювання ===");
                foreach (var group in ReportService.TopUnits(items, 3))
                    Console.WriteLine($"{group.Key,-8} позицій: {group.Count,3} сумарно: {group.TotalQuantity,6}");
                break;
            case "report-deferred":
                Console.WriteLine("=== Демонстрація відкладеного виконання ===");
                var result = ReportService.DeferredExecutionDemo(items);
                Console.WriteLine("Відкладене виконання (Where):");
                Console.WriteLine($"До додавання: {result.Before}");
                Console.WriteLine($"Після додавання: {result.After}");
                Console.WriteLine($"Знімок після очищення: {result.Snapshot}");
                Console.WriteLine();
                Console.WriteLine("Звичайний список (ToList):");
                Console.WriteLine($"До додавання: {result.ListBefore}");
                Console.WriteLine($"Після додавання: {result.ListAfter}");
                Console.WriteLine($"Після очищення: {result.ListSnapshot}");
                break;
            default:
                Console.WriteLine("Доступні команди:");
                Console.WriteLine("report-top, report-low, report-groups, report-sum");
                Console.WriteLine("report-top-units, report-deferred");
                Console.WriteLine("import [шлях] — асинхронний імпорт JSON");
                Console.WriteLine("import-stream [шлях] — потоковий імпорт JSON");
                Console.WriteLine("export — експорт до Storage:DataPath/Storage:FileName");
                Console.WriteLine("--file — файлове сховище; --timeout-seconds N — тайм-аут імпорту");
                break;
        }
        return 0;
    }
    catch (DomainException ex)
    {
        Console.WriteLine($"Помилка даних: {ex.Message}");
        return 1;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
    {
        Console.WriteLine($"Помилка файлу або конфігурації: {ex.Message}");
        return 1;
    }
}

static async Task<int> RunImportAsync(CatalogService service, string path,
    bool streaming, int timeoutSeconds)
{
    using var userCts = new CancellationTokenSource();
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        userCts.Token, timeoutCts.Token);

    ConsoleCancelEventHandler cancelHandler = (_, e) =>
    {
        e.Cancel = true;
        userCts.Cancel();
    };
    Console.CancelKeyPress += cancelHandler;
    try
    {
        var progress = new InlineProgress<int>(count =>
            Console.Write($"\rОпрацьовано записів: {count}"));
        Console.WriteLine(streaming ? "Режим: потоковий імпорт" : "Режим: асинхронний імпорт");

        int imported = streaming
            ? await service.ImportStreamingAsync(new JsonStreamingCatalogImporter(),
                path, linkedCts.Token, progress)
            : await service.ImportAsync(new JsonCatalogImporter(),
                path, linkedCts.Token, progress);

        Console.WriteLine();
        Console.WriteLine("Імпорт успішно завершено.");
        Console.WriteLine($"Додано нових товарів: {imported}");
        Console.WriteLine($"Товарів у сховищі: {service.All().Count}");
        return 0;
    }
    catch (OperationCanceledException) when (
        timeoutCts.IsCancellationRequested && !userCts.IsCancellationRequested)
    {
        Console.WriteLine();
        Console.WriteLine($"Імпорт скасовано: перевищено тайм-аут {timeoutSeconds} с.");
        return 2;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine();
        Console.WriteLine("Імпорт скасовано користувачем.");
        return 2;
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Файл не знайдено: {ex.FileName}");
        return 1;
    }
    catch (DomainException ex)
    {
        Console.WriteLine();
        Console.WriteLine($"Помилка даних: {ex.Message}");
        return 1;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Console.WriteLine();
        Console.WriteLine($"Помилка імпорту: {ex.Message}");
        return 1;
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

static void Print(IReadOnlyList<StockLine> lines)
{
    if (lines.Count == 0)
    {
        Console.WriteLine("Записів не знайдено.");
        return;
    }
    foreach (var line in lines)
        Console.WriteLine($"{line.Sku,-10} {line.Name,-25} {line.Quantity,7} {line.Unit}");
}

sealed class InlineProgress<T>(Action<T> callback) : IProgress<T>
{
    public void Report(T value) => callback(value);
}