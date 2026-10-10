using Core;
using Core.Dto;
using Core.Import;
using Core.Domain;

string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine(
        $"Файл не знайдено: {Path.GetFullPath(path)}");

    return 1;
}

ImportResult<ProductDto> result =
    Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => ProductCsvImporter.Load(path),
        ".json" => ProductJsonImporter.Load(path),
        _ => new ImportResult<ProductDto>(
            [],
            [$"Непідтримуваний формат файлу: {Path.GetExtension(path)}"])
    };

if (Path.GetFileName(path)
    .Equals("mixed.csv", StringComparison.OrdinalIgnoreCase))
{
    ImportResult<object> mixedResult =
        MixedCsvImporter.Load(path);

    Console.WriteLine(
        $"Завантажено записів: {mixedResult.Items.Count}");

    foreach (object item in mixedResult.Items)
    {
        switch (item)
        {
            case ProductDto dto:
                Console.WriteLine(
                    $"Товар: {dto.Id,-6} " +
                    $"{dto.Name,-25} " +
                    $"{dto.Quantity} {dto.Unit}");
                break;

            case WarehouseDto warehouse:
                Console.WriteLine(
                    $"Склад: {warehouse.Id,-6} " +
                    $"{warehouse.Name,-20} " +
                    $"{warehouse.City}");
                break;
        }
    }

    if (mixedResult.Errors.Count > 0)
    {
        Console.WriteLine(
            $"Пропущено рядків: {mixedResult.Errors.Count}");

        foreach (string error in mixedResult.Errors)
            Console.WriteLine($" ! {error}");
    }

    return 0;
}

int total = result.Items.Count + result.Errors.Count;
int accepted = result.Items.Count;
int skipped = result.Errors.Count;

double errorPercent =
    total == 0
        ? 0
        : skipped * 100.0 / total;

Console.WriteLine(
    $"Статистика: усього {total}, " +
    $"прийнято {accepted}, " +
    $"пропущено {skipped}, " +
    $"помилок {errorPercent:F1}%");

Console.WriteLine(
    $"Завантажено записів: {result.Items.Count}");

Console.WriteLine();

foreach (ProductDto dto in result.Items.Take(5))
{
    Console.WriteLine(
        $" {dto.Id,-6} " +
        $"{dto.Sku,-10} " +
        $"{dto.Name,-30} " +
        $"{dto.Quantity,5} " +
        $"{dto.Unit}");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine(
        $"Пропущено рядків: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($" ! {error}");
    }
}

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: успіх ===");

Product product = Product.Create(
    "P-001",
    "sku-001",
    "Цемент М400 25кг",
    "шт",
    100);

Console.WriteLine(product);

product.RegisterArrival(50);
product.Issue(30);

Console.WriteLine(product);

Console.WriteLine();

Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

TryDo(
    "видача більша за залишок",
    () => product.Issue(1000));

TryDo(
    "порожній SKU",
    () => Product.Create(
        "P-002",
        " ",
        "Пісок",
        "т",
        10));

TryDo(
    "від'ємний залишок",
    () => Product.Create(
        "P-003",
        "SKU-003",
        "Цегла",
        "шт",
        -5));

TryDo(
    "нульовий прихід",
    () => product.RegisterArrival(0));

Console.WriteLine();
Console.WriteLine($"Стан після всіх відмов: {product}");

static void TryDo(string title, Action action)
{
    try
    {
        action();

        Console.WriteLine(
            $" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}


DomainImportResult<Product> domainResult =
    ProductImportService.Convert(result);

Console.WriteLine();
Console.WriteLine("=== Доменна модель ===");

Console.WriteLine(
    $"Створено Product: {domainResult.Items.Count}");

foreach (Product productItem in domainResult.Items.Take(5))
{
    Console.WriteLine($" {productItem}");
}

if (domainResult.Errors.Count > 0)
{
    Console.WriteLine(
        $"Помилок: {domainResult.Errors.Count}");

    foreach (string error in domainResult.Errors)
        Console.WriteLine($" ! {error}");
}

Console.WriteLine();
Warehouse warehousetest = Warehouse.Create(
    "W-001",
    "Основний склад",
    "Львів");

Product producttest = Product.Create(
    "P-001",
    "SKU-001",
    "Цемент",
    "шт",
    100);

warehousetest.AddProduct(producttest);

TryDo(
    "додати той самий товар повторно",
    () => warehousetest.AddProduct(producttest));

TryDo(
    "видати товар, якого немає",
    () => warehousetest.IssueProduct("SKU-999", 10));

Product newproduct = Product.Create(
    "P-001",
    "SKU-001",
    "Цемент",
    "шт",
    100);

Console.WriteLine();
Console.WriteLine(newproduct.Status);

newproduct.ChangeStatus(ProductStatus.Suspended);

Console.WriteLine(newproduct.Status);

newproduct.ChangeStatus(ProductStatus.Active);

Console.WriteLine(newproduct.Status);

return 0;