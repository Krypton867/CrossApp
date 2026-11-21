namespace Core.Configuration;

public sealed class AppOptions
{
    public StorageOptions Storage { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public WarehouseOptions Warehouse { get; set; } = new();
    public string EnvironmentName { get; set; } = "Production";
}

public sealed class StorageOptions
{
    public string DataPath { get; set; } = "data";
    public string FileName { get; set; } = "items.json";
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; set; } = "Information";
}

public sealed class WarehouseOptions
{
    public string DefaultUnit { get; set; } = "шт";
}