using Core.Abstractions;
using Core.Reports;
using Core.Services;
using Core.Storage;

Console.WriteLine("CrossApp — лабораторна робота 7");
Console.WriteLine("Звіти через LINQ");
Console.WriteLine();

ICatalogStore store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
Console.WriteLine();

IReadOnlyList<Core.Domain.Product> items = service.All();

// Прапорці на кшталт --file не вважаються назвами команд.
string command = args.FirstOrDefault(
    a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "report-top";

switch (command)
{
    case "report-top":
        {
            Console.WriteLine("=== ТОП-5 товарів за залишком ===");

            Print(ReportService.TopByQuantity(items, 5));
            break;
        }

    case "report-low":
        {
            Console.WriteLine("=== Товари з залишком менше 10 ===");

            Print(ReportService.LowStock(items, 10));
            break;
        }

    case "report-groups":
        {
            Console.WriteLine("=== Групування за одиницями вимірювання ===");

            foreach (var group in ReportService.ByUnit(items))
            {
                Console.WriteLine(
                    $"{group.Key,-8} позицій: {group.Count,3} " +
                    $"сумарно: {group.TotalQuantity,6}");
            }

            break;
        }

    case "report-sum":
        {
            Console.WriteLine("=== Загальна статистика ===");

            var totals = ReportService.Totals(items);

            Console.WriteLine($"Кількість позицій: {totals.Items}");
            Console.WriteLine($"Загальний залишок: {totals.Total}");
            Console.WriteLine($"Середній залишок:  {totals.Average:F2}");
            Console.WriteLine($"Максимальний залишок: {totals.Max}");

            break;
        }

    case "report-top-units":
        {
            Console.WriteLine("=== ТОП-3 одиниць вимірювання ===");

            foreach (var group in ReportService.TopUnits(items, 3))
            {
                Console.WriteLine(
                    $"{group.Key,-8} позицій: {group.Count,3} " +
                    $"сумарно: {group.TotalQuantity,6}");
            }

            break;
        }

    case "report-deferred":
        {
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
        }

    default:
        {
            Console.WriteLine("Доступні команди:");
            Console.WriteLine("report-top         — топ-5 товарів");
            Console.WriteLine("report-low         — товари з низьким залишком");
            Console.WriteLine("report-groups      — групування за одиницями");
            Console.WriteLine("report-sum         — загальна статистика");
            Console.WriteLine("report-top-units   — топ одиниць вимірювання");
            Console.WriteLine("report-deferred    — відкладене виконання LINQ");
            break;
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
    {
        Console.WriteLine(
            $"{line.Sku,-10} {line.Name,-25} " +
            $"{line.Quantity,7} {line.Unit}");
    }
}

return 0;