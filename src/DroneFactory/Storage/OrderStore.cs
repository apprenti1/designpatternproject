using System.Text.Json;
using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// JSON-backed <see cref="IOrderRepository"/>. When constructed without a data directory it stays
/// purely in-memory (used by the legacy single-factory <c>InstructionHandler</c> constructor kept
/// for the phase 1/2 test suite) — orders aren't central enough to require persistence in that path.
/// </summary>
public sealed class OrderStore : IOrderRepository
{
    private readonly string? _path;
    private readonly Dictionary<string, StockOrder> _orders = new();
    private int _nextId = 1;

    public OrderStore()
        : this(null)
    {
    }

    public OrderStore(string? dataDirectory)
    {
        _path = dataDirectory is null ? null : Path.Combine(dataDirectory, "orders.json");

        if (_path is not null && File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var data = JsonSerializer.Deserialize<OrderFileData>(json);
            if (data is not null)
            {
                _nextId = data.NextId;
                foreach (var order in data.Orders)
                {
                    _orders[order.Id] = order;
                }
            }
        }
    }

    public static OrderStore CreateDefault() => new(RepoPaths.DataDirectory);

    public IReadOnlyList<StockOrder> All => _orders.Values.ToList();

    public StockOrder? Find(string id) => _orders.GetValueOrDefault(id);

    public string Create(Dictionary<string, int> remaining)
    {
        var id = $"ORDER{_nextId++}";
        _orders[id] = new StockOrder(id, remaining);
        Save();
        return id;
    }

    public void UpdateRemaining(string id, Dictionary<string, int> remaining)
    {
        _orders[id] = new StockOrder(id, remaining);
        Save();
    }

    public void Remove(string id)
    {
        _orders.Remove(id);
        Save();
    }

    private void Save()
    {
        if (_path is null)
        {
            return;
        }

        var data = new OrderFileData(_nextId, _orders.Values.ToList());
        File.WriteAllText(_path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed record OrderFileData(int NextId, List<StockOrder> Orders);
}
