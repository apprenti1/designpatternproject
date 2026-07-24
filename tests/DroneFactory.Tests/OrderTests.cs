using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>readme.md §5.2.2: ORDER / SEND / LIST_ORDER.</summary>
public class OrderTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public OrderTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(
            Path.Combine(_dataDirectory, "stock.seed.json"),
            JsonSerializer.Serialize(new Dictionary<string, int> { ["DXF-1"] = 5 }));

        _handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void OrderReturnsAUniqueIncrementingId()
    {
        var first = _handler.Order("1 DXF-1").ToList();
        var second = _handler.Order("1 DXF-1").ToList();

        Assert.Equal(new[] { "ORDER1" }, first);
        Assert.Equal(new[] { "ORDER2" }, second);
    }

    [Fact]
    public void OrderRejectsAnUnknownDrone()
    {
        var result = _handler.Order("1 Cat").ToList();

        Assert.Equal(new[] { "ERROR `Cat` is not a recognized drone" }, result);
    }

    [Fact]
    public void SendPartiallyReportsRemaining()
    {
        _handler.Order("2 DXF-1").ToList();

        var result = _handler.Send("ORDER1, 1 DXF-1").ToList();

        Assert.Equal(new[] { "Remaining for ORDER1 : 1 DXF-1" }, result);
        Assert.Contains("4 DXF-1", _handler.Stocks());
    }

    [Fact]
    public void SendCompletesTheOrderAndRemovesItFromListOrder()
    {
        _handler.Order("1 DXF-1").ToList();

        var result = _handler.Send("ORDER1, 1 DXF-1").ToList();

        Assert.Equal(new[] { "COMPLETED ORDER1" }, result);
        Assert.Empty(_handler.ListOrder());
    }

    [Fact]
    public void SendRejectsAnUnknownOrder()
    {
        var result = _handler.Send("ORDER99, 1 DXF-1").ToList();

        Assert.Equal(new[] { "ERROR `ORDER99` is not a recognized order" }, result);
    }

    [Fact]
    public void SendRejectsSendingMoreThanRemaining()
    {
        _handler.Order("1 DXF-1").ToList();

        var result = _handler.Send("ORDER1, 2 DXF-1").ToList();

        Assert.Equal(new[] { "ERROR Cannot send 2 DXF-1: only 1 remaining for `ORDER1`" }, result);
    }

    [Fact]
    public void ListOrderShowsEveryOutstandingOrder()
    {
        _handler.Order("2 DXF-1").ToList();

        Assert.Equal(new[] { "ORDER1: 2 DXF-1" }, _handler.ListOrder().ToList());
    }
}
