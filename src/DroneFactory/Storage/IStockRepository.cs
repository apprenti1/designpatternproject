namespace DroneFactory.Storage;

/// <summary>
/// Persistence boundary for item quantities (drones and pieces). Formalized as an interface so
/// <see cref="Commands.InstructionHandler"/> can be tested without real file I/O, and so a future
/// multi-factory module (readme.md §5.2.4) can give each factory its own repository instance
/// without changing any calling code — see docs/DESIGN_PATTERNS.md.
/// </summary>
public interface IStockRepository
{
    int GetQuantity(string item);

    bool HasAtLeast(IReadOnlyDictionary<string, int> required);

    void Consume(IReadOnlyDictionary<string, int> items);

    void Add(string item, int quantity);

    void Save();
}
