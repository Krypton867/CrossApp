using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Product>> LoadAsync(
        string path,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        ct.ThrowIfCancellationRequested();

        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        List<ProductDto>? dtos;

        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(
                stream,
                Options,
                ct);
        }
        catch (JsonException ex)
        {
            throw new DomainException(
                "Файл містить некоректний JSON.",
                ex);
        }

        if (dtos is null)
        {
            throw new DomainException(
                "Файл не містить списку товарів.");
        }

        List<Product> products = [];
        HashSet<string> ids = new(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            ProductDto? dto = dtos[i];

            if (dto is null)
            {
                throw new DomainException(
                    $"Запис №{i + 1} не може бути порожнім.");
            }

            try
            {
                Product product = Product.FromDto(dto);

                if (!ids.Add(product.Id))
                {
                    throw new DomainException(
                        $"У файлі є повторний ID: {product.Id}.");
                }

                products.Add(product);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException(
                    $"Помилка в записі №{i + 1}: {ex.Message}",
                    ex);
            }
        }

        return products;
    }
}