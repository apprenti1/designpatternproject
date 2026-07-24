using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>readme.md §5.2.1: WITH/WITHOUT/REPLACE drone modifiers, ';'-separated when used.</summary>
public class DroneModifierTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionHandler _handler;

    public DroneModifierTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(new Dictionary<string, int>()));

        _handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    [Fact]
    public void ClassicCommaFormatStillSumsDuplicatesUnaffectedByModifierParsing()
    {
        var lines = _handler.NeededStocks("1 DXF-1, 2 RDL-1, 3 DXF-1").ToList();

        // 4 DXF-1 + 2 RDL-1, same guarantee as readme.md §3.1 pre-phase-3.
        Assert.Equal("4 DXF-1 :", lines[0]);
    }

    [Fact]
    public void WithAddsAnExtraGenerator()
    {
        var lines = _handler.NeededStocks("1 DXF-1 WITH 1 Generator_GF1").ToList();

        Assert.Contains("2 Generator_GF1", lines);
    }

    [Fact]
    public void ReplaceSwapsAPiece()
    {
        // Processor_PU1 (2D,3D) is still compatible with System_S3D1 (control needs a tag in common).
        var lines = _handler.NeededStocks("1 DXF-1 REPLACE 1 Processor_P3D1, 1 Processor_PU1").ToList();

        Assert.Contains("1 Processor_PU1", lines);
        Assert.DoesNotContain(lines, l => l.EndsWith("Processor_P3D1"));
    }

    [Fact]
    public void MultipleEntriesComposeWithSemicolonSeparator()
    {
        var lines = _handler.NeededStocks("1 DXF-1 WITH 1 Generator_GF1; 1 RDL-1").ToList();

        Assert.Equal("1 DXF-1 :", lines[0]);
        Assert.Contains(lines, l => l.EndsWith("RDL-1 :"));
    }

    [Fact]
    public void WithoutRejectsRemovingMoreThanPresent()
    {
        var result = _handler.Verify("1 DXF-1 WITHOUT 2 Generator_GF1").ToList();

        Assert.Equal(new[] { "ERROR Cannot remove 2 Generator_GF1: only 1 present in this drone's composition" }, result);
    }

    [Fact]
    public void ReplaceRequiresAnEvenNumberOfEntries()
    {
        var result = _handler.Verify("1 DXF-1 REPLACE 1 Processor_P3D1").ToList();

        Assert.Equal(new[] { "ERROR REPLACE requires pairs of '<quantity> <Piece>' entries (one removed, one added)" }, result);
    }

    [Fact]
    public void RejectsAnUnknownDroneInModifierMode()
    {
        var result = _handler.Verify("1 Cat WITH 1 Hull_HF1").ToList();

        Assert.Equal(new[] { "ERROR `Cat` is not a recognized drone" }, result);
    }
}
