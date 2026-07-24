using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>
/// readme.md §5.1.2: up to 2 generators and 3 movement modules per drone, with the rule that
/// 2+ movement modules require exactly 2 generators.
/// </summary>
public class ExtendedConstructionTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public ExtendedConstructionTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(new Dictionary<string, int>()));

        _handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void AcceptsTwoGeneratorsAndTwoMovementModules()
    {
        var result = _handler.AddTemplate(
            "TwinDrone, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "TEMPLATE_ADDED TwinDrone" }, result);
    }

    [Fact]
    public void AcceptsThreeMovementModulesWithTwoGenerators()
    {
        var result = _handler.AddTemplate(
            "TripleMove, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "TEMPLATE_ADDED TripleMove" }, result);
    }

    [Fact]
    public void RejectsTwoMovementModulesWithOnlyOneGenerator()
    {
        var result = _handler.AddTemplate(
            "BadDrone, Hull_HF1, Core_C3D1, Generator_GF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR A template with 2 or more movement modules must have exactly 2 generators" }, result);
    }

    [Fact]
    public void RejectsThreeGenerators()
    {
        var result = _handler.AddTemplate(
            "TooManyGens, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR A template can have at most 2 generators, got 3" }, result);
    }

    [Fact]
    public void RejectsFourMovementModules()
    {
        var result = _handler.AddTemplate(
            "TooManyMoves, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR A template can have at most 3 movement modules, got 4" }, result);
    }

    [Fact]
    public void NeededStocksAggregatesDuplicatePiecesForMultiPartTemplates()
    {
        _handler.AddTemplate(
            "TwinDrone, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        var lines = _handler.NeededStocks("1 TwinDrone").ToList();

        Assert.Contains("2 Generator_GF1", lines);
        Assert.Contains("2 Move_MF1", lines);
    }

    [Fact]
    public void InstructionsGroupsGetOutStockByPieceForMultiPartTemplates()
    {
        _handler.AddTemplate(
            "TwinDrone, Hull_HF1, Core_C3D1, Generator_GF1, Generator_GF1, Move_MF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        var lines = _handler.Instructions("1 TwinDrone").ToList();

        Assert.Contains("GET_OUT_STOCK 2 Generator_GF1", lines);
        Assert.Contains("GET_OUT_STOCK 2 Move_MF1", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("GET_OUT_STOCK 1 Generator_GF1"));
    }
}
