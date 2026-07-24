using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>readme.md §5.2.4: multiple factories, TRANSFER, and the IN qualifier.</summary>
public class MultiFactoryTests : IDisposable
{
    private readonly string _dataDirectory;

    public MultiFactoryTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    private InstructionHandler CreateHandler(Dictionary<string, int> usine1Seed, Dictionary<string, int> usine2Seed)
    {
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(usine1Seed));
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.usine2.seed.json"), JsonSerializer.Serialize(usine2Seed));

        var factories = new FactoryStore(new Dictionary<string, IStockRepository>
        {
            ["Usine1"] = new StockStore(_dataDirectory),
            ["Usine2"] = new StockStore(_dataDirectory, "stock.usine2"),
        });

        return new InstructionHandler(factories, new TemplateStore(_dataDirectory), new OrderStore());
    }

    [Fact]
    public void StocksWithInShowsOnlyThatFactory()
    {
        var handler = CreateHandler(
            new Dictionary<string, int> { ["Hull_HF1"] = 5 },
            new Dictionary<string, int> { ["Hull_HF1"] = 2 });

        Assert.Contains("5 Hull_HF1", handler.Stocks("IN Usine1"));
        Assert.Contains("2 Hull_HF1", handler.Stocks("IN Usine2"));
    }

    [Fact]
    public void StocksWithoutInAggregatesEveryFactory()
    {
        var handler = CreateHandler(
            new Dictionary<string, int> { ["Hull_HF1"] = 5 },
            new Dictionary<string, int> { ["Hull_HF1"] = 2 });

        Assert.Contains("7 Hull_HF1", handler.Stocks(string.Empty));
    }

    [Fact]
    public void TransferMovesStockBetweenFactories()
    {
        var handler = CreateHandler(
            new Dictionary<string, int> { ["Hull_HF1"] = 5 },
            new Dictionary<string, int> { ["Hull_HF1"] = 0 });

        var result = handler.Transfer("Usine1, Usine2, 3 Hull_HF1").ToList();

        Assert.Equal(new[] { "STOCK_UPDATED" }, result);
        Assert.Contains("2 Hull_HF1", handler.Stocks("IN Usine1"));
        Assert.Contains("3 Hull_HF1", handler.Stocks("IN Usine2"));
    }

    [Fact]
    public void TransferRejectsInsufficientStock()
    {
        var handler = CreateHandler(
            new Dictionary<string, int> { ["Hull_HF1"] = 1 },
            new Dictionary<string, int>());

        var result = handler.Transfer("Usine1, Usine2, 3 Hull_HF1").ToList();

        Assert.Equal(new[] { "ERROR Insufficient stock to transfer" }, result);
    }

    [Fact]
    public void TransferRejectsAnUnknownFactory()
    {
        var handler = CreateHandler(new Dictionary<string, int>(), new Dictionary<string, int>());

        var result = handler.Transfer("Usine1, UsineX, 1 Hull_HF1").ToList();

        Assert.Equal(new[] { "ERROR Unknown factory `UsineX`" }, result);
    }

    [Fact]
    public void TransferRejectsTheSameFactoryTwice()
    {
        var handler = CreateHandler(new Dictionary<string, int>(), new Dictionary<string, int>());

        var result = handler.Transfer("Usine1, Usine1, 1 Hull_HF1").ToList();

        Assert.Equal(new[] { "ERROR Source and destination factory must be different" }, result);
    }

    [Fact]
    public void ProduceListsOnlyFactoriesWithSufficientStockWhenAmbiguous()
    {
        var dxf1Parts = new Dictionary<string, int>
        {
            ["Hull_HF1"] = 1,
            ["Core_C3D1"] = 1,
            ["Generator_GF1"] = 1,
            ["Move_MF1"] = 1,
            ["Processor_P3D1"] = 1,
        };
        var handler = CreateHandler(dxf1Parts, new Dictionary<string, int>());

        var result = handler.Produce("1 DXF-1").ToList();

        Assert.Equal(new[] { "ERROR Missing target factory. Available factory for this instruction are Usine1" }, result);
    }

    [Fact]
    public void ProduceReportsInsufficientStockEverywhereWhenNoFactoryQualifies()
    {
        var handler = CreateHandler(new Dictionary<string, int>(), new Dictionary<string, int>());

        var result = handler.Produce("1 DXF-1").ToList();

        Assert.Equal(new[] { "ERROR Insufficient stock to produce this order in any factory" }, result);
    }

    [Fact]
    public void ProduceWithExplicitInTargetsThatFactoryOnly()
    {
        var dxf1Parts = new Dictionary<string, int>
        {
            ["Hull_HF1"] = 1,
            ["Core_C3D1"] = 1,
            ["Generator_GF1"] = 1,
            ["Move_MF1"] = 1,
            ["Processor_P3D1"] = 1,
        };
        var handler = CreateHandler(dxf1Parts, new Dictionary<string, int>());

        var result = handler.Produce("1 DXF-1 IN Usine2").ToList();

        Assert.Equal(new[] { "ERROR Insufficient stock to produce this order" }, result);
    }
}
