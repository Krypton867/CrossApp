using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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
            List<ProductDto> dtos;
            try
            {
                string json = File.ReadAllText(_path);
                dtos = JsonSerializer.Deserialize<List<ProductDto>>(json, Options)
                    ?? throw new DomainException("Файл каталогу не містить масиву товарів.");
            }
            catch (JsonException ex)
            {
                throw new DomainException("Файл каталогу містить некоректний JSON.", ex);
            }

            foreach (ProductDto dto in dtos)
            {
                if (dto is null)
                    throw new DomainException("Файл каталогу містить порожній запис.");

                Product product;
                try
                {
                    product = Product.FromDto(dto);
                }
                catch (ArgumentException ex)
                {
                    throw new DomainException($"Некоректний товар у файлі каталогу: {ex.Message}", ex);
                }

                if (!_cache.TryAdd(product.Id, product))
                    throw new DomainException($"У файлі є дубль ID: {product.Id}.");
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        string directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);

        List<ProductDto> dtos = _cache.Values
            .Select(product => product.ToDto())
            .ToList();

        string json = JsonSerializer.Serialize(dtos, Options);
        string temporaryPath = _path + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
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
            throw new DomainException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new DomainException($"Запис з id={item.Id} не знайдено.");

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