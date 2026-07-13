using System.Text.Json;
using DroneFactory.Assembly;
using DroneFactory.Commands;
using DroneFactory.Domain;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

public class InstructionHandlerTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public InstructionHandlerTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);

        // DXF-1's parts are fully stocked; RDL-1's parts are not, to exercise the insufficient-stock paths.
        var seed = new Dictionary<string, int>
        {
            ["DXF-1"] = 0,
            ["RDL-1"] = 0,
            ["Hull_HF1"] = 10,
            ["Core_C3D1"] = 10,
            ["Generator_GF1"] = 10,
            ["Move_MF1"] = 10,
            ["Processor_P3D1"] = 10,
            ["Hull_HG1"] = 0,
            ["Core_CG1"] = 0,
            ["Generator_GG1"] = 0,
            ["Move_ML1"] = 0,
            ["Processor_PG1"] = 0,
        };
        File.WriteAllText(
            Path.Combine(_dataDirectory, "stock.seed.json"),
            JsonSerializer.Serialize(seed));

        _handler = new InstructionHandler(new StockStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void StocksListsEveryDroneThenEveryPiece()
    {
        var lines = _handler.Stocks().ToList();

        Assert.Equal(DroneCatalog.All.Count + PieceCatalog.All.Count(), lines.Count);
        Assert.Equal("0 DXF-1", lines[0]);
        Assert.Contains("10 Hull_HF1", lines);
        Assert.Contains("0 Hull_HS1", lines); // not in the seed file -> defaults to 0
    }

    [Fact]
    public void NeededStocksListsPiecesPerDroneAndTotal()
    {
        var lines = _handler.NeededStocks("1 DXF-1").ToList();

        Assert.Equal("1 DXF-1 :", lines[0]);
        Assert.Contains("1 Hull_HF1", lines);
        Assert.Contains("1 Core_C3D1", lines);
        Assert.Contains("1 Generator_GF1", lines);
        Assert.Contains("1 Move_MF1", lines);
        Assert.Contains("1 Processor_P3D1", lines);
        Assert.Equal("Total :", lines[^6]);
    }

    [Fact]
    public void NeededStocksReturnsErrorForUnknownDrone()
    {
        var lines = _handler.NeededStocks("1 Cat").ToList();

        Assert.Equal(new[] { "ERROR `Cat` is not a recognized drone" }, lines);
    }

    [Fact]
    public void InstructionsMatchesTheAssemblyPlanner()
    {
        var expected = AssemblyPlanner.BuildInstructions(DroneCatalog.Find("DXF-1")!).ToList();

        var actual = _handler.Instructions("1 DXF-1").ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void VerifyReturnsAvailableWhenPieceStockIsSufficient()
    {
        Assert.Equal(new[] { "AVAILABLE" }, _handler.Verify("1 DXF-1").ToList());
    }

    [Fact]
    public void VerifyReturnsUnavailableWhenPieceStockIsInsufficient()
    {
        Assert.Equal(new[] { "UNAVAILABLE" }, _handler.Verify("1 RDL-1").ToList());
    }

    [Fact]
    public void VerifyReturnsErrorForUnknownDrone()
    {
        // readme.md §7.2: `VERIFY 1 DXF-1, 1 Cat` -> `` ERROR `Cat` is not a recognized drone ``
        Assert.Equal(new[] { "ERROR `Cat` is not a recognized drone" }, _handler.Verify("1 DXF-1, 1 Cat").ToList());
    }

    [Fact]
    public void ProduceConsumesPiecesAndCreditsTheDroneOnSuccess()
    {
        var result = _handler.Produce("1 DXF-1").ToList();

        Assert.Equal(new[] { "STOCK_UPDATED" }, result);
        Assert.Equal(new[] { "9 Hull_HF1" }, _handler.Stocks().Where(l => l.EndsWith("Hull_HF1")).ToList());
        Assert.Equal(new[] { "1 DXF-1" }, _handler.Stocks().Where(l => l.EndsWith("DXF-1")).ToList());
    }

    [Fact]
    public void ProduceReturnsErrorAndLeavesStockUntouchedWhenPiecesAreMissing()
    {
        var before = _handler.Stocks().ToList();

        var result = _handler.Produce("1 RDL-1").ToList();

        Assert.Equal(new[] { "ERROR Insufficient stock to produce this order" }, result);
        Assert.Equal(before, _handler.Stocks().ToList());
    }
}
