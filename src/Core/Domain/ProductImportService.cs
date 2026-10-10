using Core.Dto;

namespace Core.Domain;

public sealed record DomainImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors);

public static class ProductImportService
{
    public static DomainImportResult<Product> Convert(
        ImportResult<ProductDto> source)
    {
        var items = new List<Product>();
        var errors = new List<string>(source.Errors);

        foreach (ProductDto dto in source.Items)
        {
            try
            {
                Product product = Product.FromDto(dto);
                items.Add(product);
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"товар {dto.Id}: {ex.Message}");
            }
        }

        return new DomainImportResult<Product>(
            items,
            errors);
    }
}