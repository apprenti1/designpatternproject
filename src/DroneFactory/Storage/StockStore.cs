using System.Text.Json;

namespace DroneFactory.Storage;

/// <summary>
/// Reads and persists item quantities (drones and pieces) as JSON.
/// The live file (<c>{fileBaseName}.json</c>) is seeded from <c>{fileBaseName}.seed.json</c> on
/// first use. <paramref name="fileBaseName"/> defaults to "stock" (the original single-factory
/// behavior); the multi-factory module (readme.md §5.2.4) gives each extra factory its own base
/// name (e.g. "stock.usine2") so factories never share a live file.
/// </summary>
public sealed class StockStore : IStockRepository
{
    private readonly string _livePath;
    private readonly Dictionary<string, int> _quantities;

    public StockStore(string dataDirectory)
        : this(dataDirectory, "stock")
    {
    }

    public StockStore(string dataDirectory, string fileBaseName)
    {
        var seedPath = Path.Combine(dataDirectory, $"{fileBaseName}.seed.json");
        _livePath = Path.Combine(dataDirectory, $"{fileBaseName}.json");

        if (!File.Exists(_livePath))
        {
            File.Copy(seedPath, _livePath);
        }

        var json = File.ReadAllText(_livePath);
        _quantities = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
    }

    public static StockStore CreateDefault() => new(RepoPaths.DataDirectory);

    public int GetQuantity(string item) => _quantities.GetValueOrDefault(item);

    public bool HasAtLeast(IReadOnlyDictionary<string, int> required)
        => required.All(entry => GetQuantity(entry.Key) >= entry.Value);

    public void Consume(IReadOnlyDictionary<string, int> items)
    {
        foreach (var (item, quantity) in items)
        {
            _quantities[item] = GetQuantity(item) - quantity;
        }
    }

    public void Add(string item, int quantity)
    {
        _quantities[item] = GetQuantity(item) + quantity;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(_quantities, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_livePath, json);
    }
}
