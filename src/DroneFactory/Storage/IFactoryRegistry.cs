namespace DroneFactory.Storage;

/// <summary>
/// Repository over the group of factories (readme.md §5.2.4): each factory owns its own
/// <see cref="IStockRepository"/>. Formalized as an interface for the same reasons as
/// <see cref="IStockRepository"/>/<see cref="ITemplateRepository"/> — testability, and so
/// <see cref="Commands.InstructionHandler"/> never depends on how many factories exist or how
/// they're persisted.
/// </summary>
public interface IFactoryRegistry
{
    IReadOnlyList<string> Names { get; }

    bool Exists(string name);

    IStockRepository GetStock(string name);

    int TotalQuantity(string item);
}
