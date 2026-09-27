using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    public static ImportResult<object> Load(string path)
    {
        var items = new List<object>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add(
                        $"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<object>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            ';',
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var sku, var name, var unit, var quantity]
                when int.TryParse(quantity, out int q) && q >= 0
                => new ParseOk(
                    new ProductDto(
                        id,
                        sku,
                        name,
                        unit,
                        q)),

            ["W", var id, var name, var city]
                when !string.IsNullOrWhiteSpace(name)
                  && !string.IsNullOrWhiteSpace(city)
                => new ParseOk(
                    new WarehouseDto(
                        id,
                        name,
                        city)),

            ["P", _, _, _, _, var quantity]
                => new ParseFailed(
                    $"некоректна кількість '{quantity}'"),

            ["W", _, _, ""]
                => new ParseFailed(
                    "місто складу не може бути порожнім"),

            [var type, ..]
                when type != "P" && type != "W"
                => new ParseFailed(
                    $"невідомий тип запису '{type}'"),

            _ => new ParseFailed(
                "некоректний формат рядка")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(object Value)
        : ParseOutcome;

    private sealed record ParseFailed(string Reason)
        : ParseOutcome;
}