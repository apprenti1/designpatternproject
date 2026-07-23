using System.Text.Json;
using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// JSON-backed <see cref="IMovementRepository"/>. Same in-memory-when-pathless fallback as
/// <see cref="OrderStore"/>.
/// </summary>
public sealed class MovementStore : IMovementRepository
{
    private readonly string? _path;
    private readonly List<Movement> _movements;

    public MovementStore()
        : this(null)
    {
    }

    public MovementStore(string? dataDirectory)
    {
        _path = dataDirectory is null ? null : Path.Combine(dataDirectory, "movements.json");
        _movements = new List<Movement>();

        if (_path is not null && File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            _movements = JsonSerializer.Deserialize<List<Movement>>(json) ?? new List<Movement>();
        }
    }

    public static MovementStore CreateDefault() => new(RepoPaths.DataDirectory);

    public IReadOnlyList<Movement> All => _movements;

    public void Record(string instruction, string args)
    {
        _movements.Add(new Movement(DateTimeOffset.UtcNow, instruction, args));
        Save();
    }

    private void Save()
    {
        if (_path is null)
        {
            return;
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_movements, new JsonSerializerOptions { WriteIndented = true }));
    }
}
