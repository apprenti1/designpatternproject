using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>readme.md §5.1.1: RECEIVE ARGS adds stock.</summary>
public class ReceiveTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public ReceiveTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(new Dictionary<string, int>()));

        _handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void AddsKnownPiecesToStock()
    {
        var result = _handler.Receive("5 Hull_HF1, 2 DXF-1").ToList();

        Assert.Equal(new[] { "STOCK_UPDATED" }, result);
        Assert.Contains("5 Hull_HF1", _handler.Stocks());
        Assert.Contains("2 DXF-1", _handler.Stocks());
    }

    [Fact]
    public void RejectsAnUnknownItem()
    {
        var result = _handler.Receive("3 Bogus_Piece").ToList();

        Assert.Equal(new[] { "ERROR `Bogus_Piece` is not a recognized piece, system or drone" }, result);
    }

    [Fact]
    public void AccumulatesAcrossMultipleReceives()
    {
        _handler.Receive("2 Hull_HF1").ToList();
        _handler.Receive("3 Hull_HF1").ToList();

        Assert.Contains("5 Hull_HF1", _handler.Stocks());
    }

    [Fact]
    public void RejectsMalformedArgs()
    {
        var result = _handler.Receive("").ToList();

        Assert.Equal(new[] { "ERROR Missing arguments" }, result);
    }
}
