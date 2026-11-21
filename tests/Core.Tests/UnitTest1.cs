using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class CatalogServiceTests
{
    [Fact]
    public void Receive_NegativeQuantity_ThrowsAndDoesNotChangeStock()
    {
        Product product = TestData.CreateProducts()[0];

        var store = new InMemoryCatalogStore(
            new[] { product });

        var service = new CatalogService(store);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Receive(product.Id, -5));

        Assert.Equal(50, service.Find(product.Id)!.Quantity);
    }

    [Fact]
    public void Add_RaisesChangedEventExactlyOnce()
    {
        var service = new CatalogService(
            new InMemoryCatalogStore());

        int eventCount = 0;
        CatalogChangedEventArgs? received = null;

        service.Changed += (_, args) =>
        {
            eventCount++;
            received = args;
        };

        Product product = service.Add(
            "TEST-001",
            "Test product",
            "шт",
            10);

        Assert.Equal(1, eventCount);
        Assert.NotNull(received);
        Assert.Equal(product.Id, received!.Id);
        Assert.Equal(CatalogChangeKind.Added, received.Kind);
        Assert.Equal(10, received.QuantityAfter);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(60, 2)]
    public void LowStock_ReturnsCorrectNumberOfProducts(
        int threshold,
        int expectedCount)
    {
        IReadOnlyList<Product> products =
            TestData.CreateProducts();

        var result = ReportService.LowStock(
            products,
            threshold);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void TopByQuantity_ReturnsProductsInDeterministicOrder()
    {
        IReadOnlyList<Product> products =
            TestData.CreateProducts();

        var result = ReportService.TopByQuantity(
            products,
            2);

        Assert.Equal(
            new[] { "SKU-003", "SKU-001" },
            result.Select(item => item.Sku));
    }

    [Fact]
    public async Task ImportAsync_PreCancelledToken_ThrowsOperationCanceled()
    {
        var service = new CatalogService(
            new InMemoryCatalogStore());

        var importer = new FixedCatalogImporter(
            TestData.CreateProducts());

        using var cts = new CancellationTokenSource();

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(
                importer,
                "unused.json",
                cts.Token));
    }

    [Fact]
    public async Task ImportAsync_SkipsExistingIdsAndReportsProgress()
    {
        Product existing = TestData.CreateProducts()[0];

        var store = new InMemoryCatalogStore(
            new[] { existing });

        var service = new CatalogService(store);

        var importer = new FixedCatalogImporter(
            TestData.CreateProducts());

        var progress = new RecordingProgress();

        int imported = await service.ImportAsync(
            importer,
            "unused.json",
            CancellationToken.None,
            progress);

        Assert.Equal(2, imported);
        Assert.Equal(3, store.List().Count);

        Assert.Equal(
            new[] { 1, 2, 3 },
            progress.Values);
    }

    [Fact]
    public async Task JsonImporter_InvalidJson_ThrowsDomainException()
    {
        string path = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(path, "[");

            var importer = new JsonCatalogImporter();

            await Assert.ThrowsAsync<DomainException>(
                () => importer.LoadAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task StreamingImport_ImportsProductsAndReportsProgress()
    {
        string path = Path.GetTempFileName();

        try
        {
            IReadOnlyList<Product> products =
                TestData.CreateProducts();

            string json = JsonSerializer.Serialize(
                products.Select(product => product.ToDto()).ToList());

            await File.WriteAllTextAsync(path, json);

            var store = new InMemoryCatalogStore();
            var service = new CatalogService(store);

            var importer = new JsonStreamingCatalogImporter();
            var progress = new RecordingProgress();

            int imported = await service.ImportStreamingAsync(
                importer,
                path,
                CancellationToken.None,
                progress);

            Assert.Equal(3, imported);
            Assert.Equal(3, store.List().Count);

            Assert.Equal(
                new[] { 1, 2, 3 },
                progress.Values);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_TimeoutTokenCancelsImport()
    {
        var service = new CatalogService(
            new InMemoryCatalogStore());

        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();

        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(
                userCts.Token,
                timeoutCts.Token);

        var importer = new BlockingCatalogImporter();

        Task<int> importTask = service.ImportAsync(
            importer,
            "unused.json",
            linkedCts.Token);

        // Скасовуємо саме токен тайм-ауту.
        timeoutCts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => importTask);

        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.False(userCts.IsCancellationRequested);
    }

    private static class TestData
    {
        public static IReadOnlyList<Product> CreateProducts()
        {
            return new[]
            {
                Product.Create(
                    "p1", "SKU-001", "Cement", "кг", 50),

                Product.Create(
                    "p2", "SKU-002", "Sand", "кг", 5),

                Product.Create(
                    "p3", "SKU-003", "Brick", "шт", 100)
            };
        }
    }

    private sealed class FixedCatalogImporter(
        IReadOnlyList<Product> products) : ICatalogImporter
    {
        public Task<IReadOnlyList<Product>> LoadAsync(
            string path,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return Task.FromResult(products);
        }
    }

    private sealed class BlockingCatalogImporter : ICatalogImporter
    {
        public async Task<IReadOnlyList<Product>> LoadAsync(
            string path,
            CancellationToken ct = default)
        {
            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                ct);

            return Array.Empty<Product>();
        }
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];

        public void Report(int value)
        {
            Values.Add(value);
        }
    }
}