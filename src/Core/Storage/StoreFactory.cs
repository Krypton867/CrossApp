using Core;
using Core.Abstractions;

namespace Core.Storage;

public static class StoreFactory
{
    public static ICatalogStore Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        ICatalogStore store;

        bool useFile = args.Contains(
            "--file",
            StringComparer.OrdinalIgnoreCase);

        if (useFile)
        {
            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "data",
                "catalog.json");

            store = new FileCatalogStore(path);
        }
        else
        {
            store = new InMemoryCatalogStore(
                SampleData.Products());
        }

        // Декоратор додає кешування до обраного сховища.
        return new CachingCatalogStore(store);
    }
}