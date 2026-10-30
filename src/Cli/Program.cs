using Core;
using Core.Dto;
using Core.Import;
using Core.Domain;
using Core.Abstractions;
using Core.Services;
using Core.Storage;


Console.WriteLine("CrossApp — лабораторна робота 5");
Console.WriteLine("Сервісний шар і сховища");
Console.WriteLine();

bool useFile = args.Contains(
    "--file",
    StringComparer.OrdinalIgnoreCase);

string dataPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "data",
    "catalog.json");

ICatalogStore store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");

if (useFile)
{
    Console.WriteLine($"Файл: {dataPath}");
}

Console.WriteLine();

// 1. Додавання товару
Console.WriteLine("=== 1. Додавання товару ===");

Product created = service.Add(
    "SKU-101",
    "Кабель UTP cat6",
    "м",
    50);

Console.WriteLine($"Створено: {created}");

// 2. Прихід товару
Console.WriteLine();
Console.WriteLine("=== 2. Прихід товару ===");

service.Receive(created.Id, 25);

Product? received = service.Find(created.Id);

Console.WriteLine($"Після приходу: {received}");

// 3. Видача товару
Console.WriteLine();
Console.WriteLine("=== 3. Видача товару ===");

service.Issue(created.Id, 10);

Product? issued = service.Find(created.Id);

Console.WriteLine($"Після видачі: {issued}");

// 4. Пошук за ID
Console.WriteLine();
Console.WriteLine("=== 4. Пошук за ID ===");

Product? found = service.Find(created.Id);

Console.WriteLine(found is not null
    ? $"Знайдено: {found}"
    : "Товар не знайдено");

// 5. Список товарів
Console.WriteLine();
Console.WriteLine("=== 5. Список товарів ===");

IReadOnlyList<Product> products = service.All();

foreach (Product product in products)
{
    Console.WriteLine(
        $"{product.Id,-10} " +
        $"{product.Sku,-10} " +
        $"{product.Name,-25} " +
        $"{product.Quantity,6} " +
        $"{product.Unit}");
}

Console.WriteLine($"Усього товарів: {products.Count}");

// 6. Пошук за умовою — додаткове завдання
Console.WriteLine();
Console.WriteLine("=== 6. Товари із залишком менше 100 ===");

IReadOnlyList<Product> lowStock = service.Search(
    product => product.Quantity < 100);

foreach (Product product in lowStock)
{
    Console.WriteLine(product);
}

// 7. Сценарій відмови
Console.WriteLine();
Console.WriteLine("=== 7. Перевірка помилок ===");

try
{
    service.Receive("NO-SUCH-ID", 10);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Помилка: {ex.Message}");
}

try
{
    store.Add(created);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Помилка: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Роботу завершено.");

return 0;