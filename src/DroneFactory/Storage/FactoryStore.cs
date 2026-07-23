namespace DroneFactory.Storage;

/// <summary>
/// In-process registry of factories, each backed by its own <see cref="IStockRepository"/>
/// (readme.md §5.2.4). Factories themselves are a fixed set wired at startup (<c>Program.cs</c>) —
/// the subject doesn't describe a CREATE_FACTORY instruction, only TRANSFER and the IN qualifier
/// on existing instructions (see docs/HYPOTHESES.md).
/// </summary>
public sealed class FactoryStore : IFactoryRegistry
{
    private readonly Dictionary<string, IStockRepository> _stocks;

    public FactoryStore(IReadOnlyDictionary<string, IStockRepository> stocks) => _stocks = new Dictionary<string, IStockRepository>(stocks);

    public IReadOnlyList<string> Names => _stocks.Keys.ToList();

    public bool Exists(string name) => _stocks.ContainsKey(name);

    public IStockRepository GetStock(string name) => _stocks[name];

    public int TotalQuantity(string item) => _stocks.Values.Sum(s => s.GetQuantity(item));
}
