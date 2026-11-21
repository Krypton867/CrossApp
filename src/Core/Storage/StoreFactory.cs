using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static ICatalogStore Create(string[] args, string filePath)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
        ICatalogStore store = useFile
            ? new FileCatalogStore(filePath)
            : new InMemoryCatalogStore(SampleData.Products());

        return new CachingCatalogStore(store);
    }

    // Зберігаємо попередній виклик для сумісності з кодом старих лабораторних.
    public static ICatalogStore Create(string[] args)
        => Create(args, Path.Combine(AppContext.BaseDirectory, "data", "items.json"));
}