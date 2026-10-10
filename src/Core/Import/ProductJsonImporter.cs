using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<ProductDto>? products =
                JsonSerializer.Deserialize<List<ProductDto>>(
                    json,
                    options);

            if (products is null)
            {
                errors.Add("JSON не містить масиву товарів");
                return new ImportResult<ProductDto>(items, errors);
            }

            for (int i = 0; i < products.Count; i++)
            {
                int number = i + 1;
                ProductDto product = products[i];

                switch (product)
                {
                    case { Id: "" }:
                        errors.Add(
                            $"запис {number}: ID не може бути порожнім");
                        break;

                    case { Sku: "" }:
                        errors.Add(
                            $"запис {number}: SKU не може бути порожнім");
                        break;

                    case { Name: "" }:
                        errors.Add(
                            $"запис {number}: назва не може бути порожньою");
                        break;

                    case { Unit: "" }:
                        errors.Add(
                            $"запис {number}: одиниця вимірювання не може бути порожньою");
                        break;

                    case { Quantity: < 0 }:
                        errors.Add(
                            $"запис {number}: кількість не може бути від'ємною");
                        break;

                    default:
                        items.Add(product);
                        break;
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Помилка JSON: {ex.Message}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}