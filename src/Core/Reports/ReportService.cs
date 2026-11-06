using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1. Товари з низьким залишком
    public static IReadOnlyList<StockLine> LowStock(
        IEnumerable<Product> source,
        int threshold)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);

        return source
            .Where(p => p.Quantity < threshold)
            .OrderBy(p => p.Quantity)
            .ThenBy(p => p.Sku, StringComparer.Ordinal)
            .Select(p => new StockLine(
                p.Sku, p.Name, p.Quantity, p.Unit))
            .ToList();
    }

    // 2. Топ-N товарів за залишком
    public static IReadOnlyList<StockLine> TopByQuantity(
        IEnumerable<Product> source,
        int n)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source
            .TopBy(p => p.Quantity, n, p => p.Sku)
            .Select(p => new StockLine(
                p.Sku, p.Name, p.Quantity, p.Unit))
            .ToList();
    }



    // 3. Групування за одиницею вимірювання
    public static IReadOnlyList<GroupSummary> ByUnit(
        IEnumerable<Product> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source
            .GroupBy(
                p => p.Unit,
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(
                g.Key,
                g.Count(),
                g.Sum(p => p.Quantity)))
            .OrderByDescending(g => g.TotalQuantity)
            .ToList();
    }

    // 4. Загальні показники
    public static (int Items, int Total, double Average, int Max)
        Totals(IEnumerable<Product> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var list = source.ToList();

        if (list.Count == 0)
            return (0, 0, 0d, 0);

        return (
            list.Count,
            list.Sum(p => p.Quantity),
            list.Average(p => p.Quantity),
            list.Max(p => p.Quantity));
    }

    // Додатковий звіт: топ одиниць вимірювання
    public static IReadOnlyList<GroupSummary> TopUnits(
        IEnumerable<Product> source,
        int n)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source
            .GroupBy(
                p => p.Unit,
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(
                g.Key,
                g.Count(),
                g.Sum(p => p.Quantity)))
            .TopBy(g => g.TotalQuantity, n, g => g.Key)
            .ToList();
    }

    // Демонстрація відкладеного виконання LINQ
    public static (
    int Before,
    int After,
    int Snapshot,
    int ListBefore,
    int ListAfter,
    int ListSnapshot
) DeferredExecutionDemo(IEnumerable<Product> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = source.ToList();

        // Відкладене виконання
        var query = items.Where(p => p.Quantity < 10);

        // Звичайний список: результат отримується одразу
        var regularList = query.ToList();

        int before = query.Count();
        int listBefore = regularList.Count;

        // Додаємо новий товар до початкової колекції
        items.Add(Product.Create(
            "tmp00001",
            "SKU-999",
            "Скоба",
            "шт",
            3));

        // Повторно отримуємо результати
        int after = query.Count();
        int listAfter = regularList.Count;

        // Створюємо знімок відкладеного запиту
        var snapshot = query.ToList();

        // Очищаємо початкову колекцію
        items.Clear();

        int snapshotCount = snapshot.Count;
        int listSnapshot = regularList.Count;

        return (
            before,
            after,
            snapshotCount,
            listBefore,
            listAfter,
            listSnapshot
        );
    }
}