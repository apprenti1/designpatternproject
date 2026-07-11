using DroneFactory.Assembly;
using DroneFactory.Domain;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Pure business logic for the five base user instructions (readme.md §3.2). Each method
/// returns the output lines to print, so it can be exercised in tests without touching Console.
/// </summary>
public sealed class InstructionHandler
{
    private readonly StockStore _stock;

    public InstructionHandler(StockStore stock)
    {
        _stock = stock;
    }

    public IEnumerable<string> Stocks()
    {
        foreach (var drone in DroneCatalog.All)
        {
            yield return $"{_stock.Get(drone.Name)} {drone.Name}";
        }

        foreach (var piece in PieceCatalog.All)
        {
            yield return $"{_stock.Get(piece.Name)} {piece.Name}";
        }
    }

    public IEnumerable<string> NeededStocks(string args)
    {
        if (!TryParseOrder(args, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        var total = new Dictionary<string, int>();

        foreach (var (droneName, quantity) in order)
        {
            var drone = DroneCatalog.Find(droneName)!;
            yield return $"{quantity} {droneName} :";

            foreach (var piece in drone.RequiredPieces)
            {
                yield return $"{quantity} {piece}";
                total[piece] = total.GetValueOrDefault(piece) + quantity;
            }
        }

        yield return "Total :";
        foreach (var (piece, quantity) in total)
        {
            yield return $"{quantity} {piece}";
        }
    }

    public IEnumerable<string> Instructions(string args)
    {
        if (!TryParseOrder(args, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        foreach (var (droneName, quantity) in order)
        {
            var drone = DroneCatalog.Find(droneName)!;
            foreach (var line in AssemblyPlanner.BuildInstructions(drone, quantity))
            {
                yield return line;
            }
        }
    }

    public IEnumerable<string> Verify(string args)
    {
        if (!TryParseOrder(args, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        yield return _stock.HasAtLeast(AggregateNeededPieces(order)) ? "AVAILABLE" : "UNAVAILABLE";
    }

    public IEnumerable<string> Produce(string args)
    {
        if (!TryParseOrder(args, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        var needed = AggregateNeededPieces(order);
        if (!_stock.HasAtLeast(needed))
        {
            yield return "ERROR Insufficient stock to produce this order";
            yield break;
        }

        _stock.Consume(needed);
        foreach (var (droneName, quantity) in order)
        {
            _stock.Add(droneName, quantity);
        }

        _stock.Save();
        yield return "STOCK_UPDATED";
    }

    private static bool TryParseOrder(string args, out Dictionary<string, int> order, out string? error)
    {
        order = new Dictionary<string, int>();
        error = null;

        if (!ArgsParser.TryParse(args, out var quantities, out var parseError))
        {
            error = parseError;
            return false;
        }

        foreach (var (name, quantity) in quantities)
        {
            if (DroneCatalog.Find(name) is null)
            {
                error = $"`{name}` is not a recognized drone";
                return false;
            }

            order[name] = quantity;
        }

        return true;
    }

    private static Dictionary<string, int> AggregateNeededPieces(Dictionary<string, int> order)
    {
        var needed = new Dictionary<string, int>();
        foreach (var (droneName, quantity) in order)
        {
            var drone = DroneCatalog.Find(droneName)!;
            foreach (var piece in drone.RequiredPieces)
            {
                needed[piece] = needed.GetValueOrDefault(piece) + quantity;
            }
        }

        return needed;
    }
}
