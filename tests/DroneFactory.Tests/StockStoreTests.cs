using DroneFactory.Storage;

namespace DroneFactory.Tests;

public class StockStoreTests : IDisposable
{
    private readonly string _dataDirectory;

    public StockStoreTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(
            Path.Combine(_dataDirectory, "stock.seed.json"),
            "{ \"DXF-1\": 0, \"Hull_HF1\": 5 }");
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void CreatesTheLiveFileFromTheSeedOnFirstUse()
    {
        var livePath = Path.Combine(_dataDirectory, "stock.json");
        Assert.False(File.Exists(livePath));

        var store = new StockStore(_dataDirectory);

        Assert.True(File.Exists(livePath));
        Assert.Equal(5, store.GetQuantity("Hull_HF1"));
        Assert.Equal(0, store.GetQuantity("DXF-1"));
    }

    [Fact]
    public void UnknownItemsDefaultToZero()
    {
        var store = new StockStore(_dataDirectory);

        Assert.Equal(0, store.GetQuantity("Nonexistent_Piece"));
    }

    [Fact]
    public void ConsumeAndAddMutateInMemoryQuantities()
    {
        var store = new StockStore(_dataDirectory);

        store.Consume(new Dictionary<string, int> { ["Hull_HF1"] = 2 });
        store.Add("DXF-1", 1);

        Assert.Equal(3, store.GetQuantity("Hull_HF1"));
        Assert.Equal(1, store.GetQuantity("DXF-1"));
    }

    [Fact]
    public void SavePersistsAcrossNewInstances()
    {
        var first = new StockStore(_dataDirectory);
        first.Consume(new Dictionary<string, int> { ["Hull_HF1"] = 5 });
        first.Add("DXF-1", 1);
        first.Save();

        var second = new StockStore(_dataDirectory);

        Assert.Equal(0, second.GetQuantity("Hull_HF1"));
        Assert.Equal(1, second.GetQuantity("DXF-1"));
    }

    [Fact]
    public void HasAtLeastReflectsCurrentStock()
    {
        var store = new StockStore(_dataDirectory);

        Assert.True(store.HasAtLeast(new Dictionary<string, int> { ["Hull_HF1"] = 5 }));
        Assert.False(store.HasAtLeast(new Dictionary<string, int> { ["Hull_HF1"] = 6 }));
    }
}
