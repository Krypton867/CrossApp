using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Product> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _path;
    private bool _loaded;

    public FileCatalogStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (File.Exists(_path))
        {
            string json = File.ReadAllText(_path);

            List<ProductDto> dtos =
                JsonSerializer.Deserialize<List<ProductDto>>(
                    json, Options) ?? [];

            foreach (ProductDto dto in dtos)
            {
                Product product = Product.FromDto(dto);

                if (_cache.ContainsKey(product.Id))
                {
                    throw new InvalidOperationException(
                        $"У файлі є дубль ID: {product.Id}.");
                }

                _cache.Add(product.Id, product);
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        string? directory = Path.GetDirectoryName(_path);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        List<ProductDto> dtos = _cache.Values
            .Select(product => product.ToDto())
            .ToList();

        string json = JsonSerializer.Serialize(dtos, Options);

        File.WriteAllText(_path, json);
    }

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();

        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        EnsureLoaded();

        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
        {
            throw new InvalidOperationException(
                $"Запис з id={item.Id} уже існує.");
        }

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);

        EnsureLoaded();

        if (!_cache.ContainsKey(item.Id))
        {
            throw new InvalidOperationException(
                $"Запис з id={item.Id} не знайдено.");
        }

        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        EnsureLoaded();

        if (!_cache.Remove(id))
            return false;

        Flush();
        return true;
    }
}