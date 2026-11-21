using System.Runtime.CompilerServices;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonStreamingCatalogImporter : IStreamingCatalogImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async IAsyncEnumerable<Product> StreamAsync(
        string path,
        [EnumeratorCancellation] CancellationToken ct = default)
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

        HashSet<string> ids = new(
            StringComparer.OrdinalIgnoreCase);

        int index = 0;

        await foreach (ProductDto? dto in
            JsonSerializer.DeserializeAsyncEnumerable<ProductDto>(
                stream,
                Options,
                ct))
        {
            ct.ThrowIfCancellationRequested();

            index++;

            if (dto is null)
            {
                throw new DomainException(
                    $"Запис №{index} не може бути порожнім.");
            }

            Product product;

            try
            {
                product = Product.FromDto(dto);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException(
                    $"Помилка в записі №{index}: {ex.Message}",
                    ex);
            }

            if (!ids.Add(product.Id))
            {
                throw new DomainException(
                    $"У файлі є повторний ID: {product.Id}.");
            }

            yield return product;
        }
    }
}