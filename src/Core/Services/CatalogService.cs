using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using System.Text.Json;

namespace Core.Services;

public sealed class CatalogService
{
    private readonly ICatalogStore _store;
    private readonly INotificationSink? _notificationSink;

    public CatalogService(
        ICatalogStore store,
        INotificationSink? notificationSink = null)
    {
        _store = store
            ?? throw new ArgumentNullException(nameof(store));

        _notificationSink = notificationSink;
    }

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(
        Product product,
        CatalogChangeKind kind,
        int delta,
        int? quantityAfter = null)
    {
        var eventArgs = new CatalogChangedEventArgs(
            product.Id,
            kind,
            delta,
            quantityAfter ?? product.Quantity);

        Changed?.Invoke(this, eventArgs);

        _notificationSink?.Notify(eventArgs);
    }

    public Product Add(
        string sku,
        string name,
        string unit,
        int quantity)
    {
        string id = Guid.NewGuid().ToString("N")[..8];

        Product product = Product.Create(
            id,
            sku,
            name,
            unit,
            quantity);

        _store.Add(product);

        OnChanged(product, CatalogChangeKind.Added, quantity);

        return product;
    }

    public void Receive(string id, int quantity)
    {
        Product product = _store.GetById(id)
            ?? throw new DomainException(
                $"Немає товару з id={id}.");

        product.RegisterArrival(quantity);

        _store.Update(product);

        OnChanged(
            product,
            CatalogChangeKind.Received,
            quantity);
    }

    public void Issue(string id, int quantity)
    {
        Product product = _store.GetById(id)
            ?? throw new DomainException(
                $"Немає товару з id={id}.");

        product.Issue(quantity);

        _store.Update(product);

        OnChanged(
            product,
            CatalogChangeKind.Issued,
            -quantity);
    }

    public IReadOnlyList<Product> All()
    {
        return _store.List();
    }

    public Product? Find(string id)
    {
        return _store.GetById(id);
    }

    public bool Remove(string id)
    {
        Product? product = _store.GetById(id);

        if (product is null)
            return false;

        bool removed = _store.Remove(id);

        if (removed)
        {
            OnChanged(
                product,
                CatalogChangeKind.Removed,
                -product.Quantity,
                0);
        }

        return removed;
    }

    public IReadOnlyList<Product> Search(
        Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _store.List()
            .Where(predicate)
            .ToList();
    }

    public IReadOnlyList<Product> FindAll(
        Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _store.List()
            .Where(predicate)
            .ToList();
    }

    public async Task<int> ImportAsync(
    ICatalogImporter importer,
    string path,
    CancellationToken ct = default,
    IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ct.ThrowIfCancellationRequested();

        IReadOnlyList<Product> products =
            await importer.LoadAsync(path, ct);

        int imported = 0;
        int processed = 0;

        foreach (Product product in products)
        {
            ct.ThrowIfCancellationRequested();

            processed++;

            // Пропускаємо товари, які вже є в каталозі.
            if (_store.GetById(product.Id) is not null)
            {
                progress?.Report(processed);
                continue;
            }

            _store.Add(product);

            OnChanged(
                product,
                CatalogChangeKind.Added,
                product.Quantity);

            imported++;

            progress?.Report(processed);
        }

        if (products.Count == 0)
            progress?.Report(0);

        return imported;
    }

    public async Task<int> ImportStreamingAsync(
    IStreamingCatalogImporter importer,
    string path,
    CancellationToken ct = default,
    IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ct.ThrowIfCancellationRequested();

        int imported = 0;
        int processed = 0;

        try
        {
            await foreach (Product product in importer.StreamAsync(path, ct))
            {
                ct.ThrowIfCancellationRequested();

                processed++;

                // Не додаємо повторні ID, які вже є в каталозі.
                if (_store.GetById(product.Id) is not null)
                {
                    progress?.Report(processed);
                    continue;
                }

                _store.Add(product);

                OnChanged(
                    product,
                    CatalogChangeKind.Added,
                    product.Quantity);

                imported++;

                progress?.Report(processed);
            }
        }
        catch (JsonException ex)
        {
            throw new DomainException(
                "Файл містить некоректний JSON.",
                ex);
        }

        if (processed == 0)
            progress?.Report(0);

        return imported;
    }
}