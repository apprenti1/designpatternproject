using System.Text.Json;
using DroneFactory.Commands;
using DroneFactory.Storage;

namespace DroneFactory.Tests;

/// <summary>
/// readme.md §5.2.3: GET_MOVEMENTS, fed by the LoggingInstruction decorator wrapping every
/// stock-impacting command (see docs/DESIGN_PATTERNS.md, "Decorator").
/// </summary>
public class MovementTests : IDisposable
{
    private readonly string _dataDirectory;
    private readonly InstructionRegistry _registry;

    public MovementTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "DroneFactoryTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "stock.seed.json"), JsonSerializer.Serialize(new Dictionary<string, int>()));

        var handler = new InstructionHandler(new StockStore(_dataDirectory), new TemplateStore(_dataDirectory));
        var movements = new MovementStore();
        _registry = new InstructionRegistry(handler, movements);
    }

    public void Dispose() => Directory.Delete(_dataDirectory, recursive: true);

    private IEnumerable<string> Run(string name, string args)
    {
        _registry.TryGet(name, out var instruction);
        return instruction.Execute(args);
    }

    [Fact]
    public void RecordsASuccessfulReceive()
    {
        Run("RECEIVE", "5 Hull_HF1").ToList();

        var movements = Run("GET_MOVEMENTS", string.Empty).ToList();

        Assert.Equal(new[] { "RECEIVE 5 Hull_HF1" }, movements);
    }

    [Fact]
    public void DoesNotRecordAFailedInstruction()
    {
        Run("RECEIVE", "5 Bogus_Piece").ToList();

        Assert.Empty(Run("GET_MOVEMENTS", string.Empty));
    }

    [Fact]
    public void FiltersByItemName()
    {
        Run("RECEIVE", "5 Hull_HF1").ToList();
        Run("RECEIVE", "3 Move_MF1").ToList();

        var filtered = Run("GET_MOVEMENTS", "Hull_HF1").ToList();

        Assert.Equal(new[] { "RECEIVE 5 Hull_HF1" }, filtered);
    }

    [Fact]
    public void DoesNotRecordInstructionsThatDoNotImpactStock()
    {
        Run("STOCKS", string.Empty).ToList();

        Assert.Empty(Run("GET_MOVEMENTS", string.Empty));
    }
}
