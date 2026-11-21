using Core;
using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class FileCatalogStoreLab09Tests
{
    [Fact]
    public void Add_CreatesDirectoryAndWritesReadableCyrillicJson()
    {
        string folder = Path.Combine(Path.GetTempPath(), "crossapp-tests", Guid.NewGuid().ToString("N"));
        string file = Path.Combine(folder, "subfolder", "items.json");
        try
        {
            var store = new FileCatalogStore(file);
            store.Add(Product.Create("p1", "SKU-1", "Цемент", "мішок", 40));

            Assert.True(File.Exists(file));
            Assert.False(File.Exists(file + ".tmp"));
            string json = File.ReadAllText(file);
            Assert.Contains("Цемент", json);
            Assert.Contains("\"quantity\": 40", json);
            Assert.DoesNotContain("\\u0426", json);

            var restored = new FileCatalogStore(file);
            Assert.Equal(40, restored.GetById("p1")!.Quantity);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void List_InvalidJson_ThrowsDomainException()
    {
        string folder = Path.Combine(Path.GetTempPath(), "crossapp-tests", Guid.NewGuid().ToString("N"));
        string file = Path.Combine(folder, "items.json");
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(file, "{invalid json");
            var store = new FileCatalogStore(file);

            Assert.Throws<DomainException>(() => store.List());
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }
}