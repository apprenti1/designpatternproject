using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// Persistence boundary for backorders (ORDER/SEND/LIST_ORDER, readme.md §5.2.2). See
/// <see cref="IStockRepository"/> for why this is an interface.
/// </summary>
public interface IOrderRepository
{
    IReadOnlyList<StockOrder> All { get; }

    StockOrder? Find(string id);

    string Create(Dictionary<string, int> remaining);

    void UpdateRemaining(string id, Dictionary<string, int> remaining);

    void Remove(string id);
}
