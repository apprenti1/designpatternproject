using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

public class AddTemplateTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public AddTemplateTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(new Dictionary<string, int>()));

        _handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void AddsAValidTemplateAndMakesItUsableEverywhereElse()
    {
        var result = _handler.AddTemplate("MyDrone, Hull_HF1, Core_C3D1, Generator_GF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "TEMPLATE_ADDED MyDrone" }, result);
        Assert.Contains("0 MyDrone", _handler.Stocks());
        Assert.Equal(new[] { "UNAVAILABLE" }, _handler.Verify("1 MyDrone").ToList());
    }

    [Fact]
    public void RejectsAnUnknownPiece()
    {
        var result = _handler.AddTemplate("MyDrone, Hull_HF1, Core_C3D1, Generator_GF1, Move_MF1, Processor_P3D1, System_Bogus").ToList();

        Assert.Equal(new[] { "ERROR `System_Bogus` is not a recognized piece or system" }, result);
    }

    [Fact]
    public void RejectsADuplicateTemplateName()
    {
        var result = _handler.AddTemplate("DXF-1, Hull_HF1, Core_C3D1, Generator_GF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR A template named `DXF-1` already exists" }, result);
    }

    [Fact]
    public void RejectsATemplateWithTwoPiecesOfTheSameSlot()
    {
        var result = _handler.AddTemplate("MyDrone, Hull_HF1, Hull_HG1, Core_C3D1, Generator_GF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR A template can only have one hull, got both `Hull_HF1` and `Hull_HG1`" }, result);
    }

    [Fact]
    public void RejectsATemplateBelongingToNoCategory()
    {
        // No (S) hull, a movement module tagged only (S), and a (2D) system: matches none of the four categories.
        var result = _handler.AddTemplate("MyDrone, Hull_HF1, Core_CG1, Generator_GF1, Move_MS1, Processor_PG1, System_SG1").ToList();

        Assert.Equal(new[] { "ERROR This combination of pieces does not belong to any drone category (Aérien, Marin, Terrestre, Submersible)" }, result);
    }

    [Fact]
    public void RejectsAnIncompatibleMainModuleAndSystem()
    {
        // Core_CG1 only supports (2D); System_S3D1 needs (2D) and (3D).
        var result = _handler.AddTemplate("MyDrone, Hull_HF1, Core_CG1, Generator_GF1, Move_MF1, Processor_P3D1, System_S3D1").ToList();

        Assert.Equal(new[] { "ERROR Main module `Core_CG1` does not support system `System_S3D1`" }, result);
    }
}
