using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;
using Cli.Notifications;

Console.WriteLine("CrossApp — лабораторна робота 6");
Console.WriteLine("Доменні події, делегати та лямбди");
Console.WriteLine();

bool useFile = args.Contains(
    "--file",
    StringComparer.OrdinalIgnoreCase);

ICatalogStore store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine();

// Основний файл журналу подій
string logPath = Path.Combine(
    AppContext.BaseDirectory,
    "logs",
    "app.log");

Directory.CreateDirectory(
    Path.GetDirectoryName(logPath)!);

// Перший підписник: консоль і файл
void OnCatalogChanged(
    object? sender,
    CatalogChangedEventArgs e)
{
    string line =
        $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} " +
        $"{e.Delta,4:+0;-0;0} → залишок {e.QuantityAfter}";

    Console.WriteLine(line);

    File.AppendAllText(
        logPath,
        line + Environment.NewLine);
}

service.Changed += OnCatalogChanged;

// Другий підписник: лямбда з замиканням
int changes = 0;

service.Changed += (_, _) => changes++;

Console.WriteLine("=== 1. Додавання товару ===");

Product created = service.Add(
    "SKU-101",
    "Кабель UTP cat6",
    "м",
    50);

Console.WriteLine($"Створено: {created}");

Console.WriteLine();
Console.WriteLine("=== 2. Прихід товару ===");

service.Receive(created.Id, 25);

Console.WriteLine($"Поточний товар: {service.Find(created.Id)}");
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== 3. Перевірка відмови ===");

// Від'ємна кількість порушує інваріант Product.
try
{
    service.Receive(created.Id, -5);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Помилка: {ex.Message}");
}

Console.WriteLine($"Подій після помилки: {changes}");

// Неіснуючий ID також не повинен породжувати подію.
try
{
    service.Receive("NO-SUCH-ID", 10);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Помилка: {ex.Message}");
}

Console.WriteLine($"Подій після пошуку відсутнього товару: {changes}");

Console.WriteLine();
Console.WriteLine("=== 4. Відписка першого підписника ===");

service.Changed -= OnCatalogChanged;

service.Receive(created.Id, 5);

Console.WriteLine($"Після відписки: {service.Find(created.Id)}");
Console.WriteLine($"Подій отримано: {changes}");

Console.WriteLine();
Console.WriteLine("=== 5. Пошук через Func<Product, bool> ===");

IReadOnlyList<Product> lowStock = service.FindAll(
    product => product.Quantity < 10);

if (lowStock.Count == 0)
{
    Console.WriteLine("Товарів із залишком менше 10 немає.");
}
else
{
    foreach (Product product in lowStock)
        Console.WriteLine(product);
}

Console.WriteLine();
Console.WriteLine("=== 6. Додаткове завдання: Action<string> ===");

string csvPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "data",
    "sample.csv");

if (File.Exists(csvPath))
{
    var importResult = ProductCsvImporter.Load(
        csvPath,
        message => Console.WriteLine($"[Імпорт] {message}"));

    Console.WriteLine(
        $"Імпортовано записів: {importResult.Items.Count}");

    Console.WriteLine(
        $"Помилок імпорту: {importResult.Errors.Count}");
}
else
{
    Console.WriteLine(
        $"Файл для демонстрації імпорту не знайдено: {csvPath}");
}

Console.WriteLine();
Console.WriteLine("=== 7. Додаткове завдання: INotificationSink ===");

// Окремий сервіс для демонстрації DI.
// Він використовує окреме сховище, щоб не змінювати
// дані основного сценарію.
ICatalogStore demoStore = new InMemoryCatalogStore();

string sinkLogPath = Path.Combine(
    AppContext.BaseDirectory,
    "logs",
    "sink-demo.log");

INotificationSink sink = new CompositeNotificationSink(
    new ConsoleSink(),
    new FileSink(sinkLogPath));

var sinkService = new CatalogService(demoStore, sink);

sinkService.Add(
    "SKU-DI-01",
    "Демонстраційний товар",
    "шт",
    1);

Console.WriteLine($"Журнал подій: {logPath}");
Console.WriteLine($"Журнал DI-демонстрації: {sinkLogPath}");

Console.WriteLine();
Console.WriteLine("Роботу завершено.");

return 0;