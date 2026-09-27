using Core;
using Core.Dto;
using Core.Import;

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
            case ProductDto product:
                Console.WriteLine(
                    $"Товар: {product.Id,-6} " +
                    $"{product.Name,-25} " +
                    $"{product.Quantity} {product.Unit}");
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

foreach (ProductDto product in result.Items.Take(5))
{
    Console.WriteLine(
        $" {product.Id,-6} " +
        $"{product.Sku,-10} " +
        $"{product.Name,-30} " +
        $"{product.Quantity,5} " +
        $"{product.Unit}");
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

return 0;