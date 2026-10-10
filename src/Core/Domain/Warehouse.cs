namespace Core.Domain;

public sealed class Warehouse
{
    private readonly List<Product> _products = [];

    public string Id { get; }
    public string Name { get; }
    public string City { get; }

    public IReadOnlyList<Product> Products => _products;

    private Warehouse(
        string id,
        string name,
        string city)
    {
        Id = id;
        Name = name;
        City = city;
    }

    public static Warehouse Create(
        string id,
        string name,
        string city)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(
                "Ідентифікатор складу обов'язковий",
                nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Назва складу не може бути порожньою",
                nameof(name));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException(
                "Місто складу не може бути порожнім",
                nameof(city));

        return new Warehouse(
            id.Trim(),
            name.Trim(),
            city.Trim());
    }

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (_products.Any(p => p.Sku == product.Sku))
        {
            throw new InvalidOperationException(
                $"Товар з SKU '{product.Sku}' вже є на складі");
        }

        _products.Add(product);
    }

    public void IssueProduct(string sku, int amount)
    {
        Product? product = _products
            .FirstOrDefault(p => p.Sku == sku);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Товар з SKU '{sku}' не знайдено на складі");
        }

        product.Issue(amount);
    }
}