using DroneFactory.Assembly;
using DroneFactory.Domain;
using DroneFactory.Domain.Categories;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Pure business logic for the user instructions (readme.md §3.2, §4.3). Each method
/// returns the output lines to print, so it can be exercised in tests without touching Console.
/// </summary>
public sealed class InstructionHandler
{
    private readonly IStockRepository _stock;
    private readonly ITemplateRepository _templates;

    public InstructionHandler(IStockRepository stock, ITemplateRepository templates)
    {
        _stock = stock;
        _templates = templates;
    }

    public IEnumerable<string> Stocks()
    {
        foreach (var drone in _templates.All)
        {
            yield return $"{_stock.GetQuantity(drone.Name)} {drone.Name}";
        }

        foreach (var piece in PieceCatalog.All)
        {
            yield return $"{_stock.GetQuantity(piece.Name)} {piece.Name}";
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
            var drone = _templates.Find(droneName)!;
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
            var drone = _templates.Find(droneName)!;
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

    public IEnumerable<string> AddTemplate(string args)
    {
        if (!TryParseTemplate(args, out var template, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        _templates.Add(template!);
        yield return $"TEMPLATE_ADDED {template!.Name}";
    }

    private bool TryParseOrder(string args, out Dictionary<string, int> order, out string? error)
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
            if (_templates.Find(name) is null)
            {
                error = $"`{name}` is not a recognized drone";
                return false;
            }

            order[name] = quantity;
        }

        return true;
    }

    private Dictionary<string, int> AggregateNeededPieces(Dictionary<string, int> order)
    {
        var needed = new Dictionary<string, int>();
        foreach (var (droneName, quantity) in order)
        {
            var drone = _templates.Find(droneName)!;
            foreach (var piece in drone.RequiredPieces)
            {
                needed[piece] = needed.GetValueOrDefault(piece) + quantity;
            }
        }

        return needed;
    }

    private bool TryParseTemplate(string args, out DroneTemplate? template, out string? error)
    {
        template = null;
        error = null;

        if (string.IsNullOrWhiteSpace(args))
        {
            error = "Missing arguments";
            return false;
        }

        var tokens = args.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            error = "Expected format 'TEMPLATE_NAME, Piece1, ..., PieceN'";
            return false;
        }

        var name = tokens[0];
        if (_templates.Find(name) is not null)
        {
            error = $"A template named `{name}` already exists";
            return false;
        }

        string? hull = null, mainModule = null, generator = null, movementModule = null, controlModule = null, system = null;

        foreach (var piece in tokens.Skip(1))
        {
            if (PieceCatalog.Hulls.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref hull, piece, "hull", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.MainModules.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref mainModule, piece, "main module", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.Generators.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref generator, piece, "generator", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.MovementModules.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref movementModule, piece, "movement module", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.ControlModules.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref controlModule, piece, "control module", out error))
                {
                    return false;
                }
            }
            else if (SystemCatalog.All.Any(s => s.Name == piece))
            {
                if (!TrySetOnce(ref system, piece, "system", out error))
                {
                    return false;
                }
            }
            else
            {
                error = $"`{piece}` is not a recognized piece or system";
                return false;
            }
        }

        if (hull is null || mainModule is null || generator is null || movementModule is null || controlModule is null || system is null)
        {
            error = "A template requires exactly one hull, one main module, one generator, one movement module, one control module and one system";
            return false;
        }

        var mainModuleTags = PieceCatalog.MainModules.First(p => p.Name == mainModule).Tags;
        var controlModuleTags = PieceCatalog.ControlModules.First(p => p.Name == controlModule).Tags;
        var systemTags = SystemCatalog.All.First(s => s.Name == system).Tags;

        if (!systemTags.All(mainModuleTags.Contains))
        {
            error = $"Main module `{mainModule}` does not support system `{system}`";
            return false;
        }

        if (!controlModuleTags.Intersect(systemTags).Any())
        {
            error = $"Control module `{controlModule}` is not compatible with system `{system}`";
            return false;
        }

        var candidate = new DroneTemplate(name, hull, mainModule, generator, movementModule, controlModule, system);
        if (CategoryClassifier.Classify(candidate) == DroneCategory.None)
        {
            error = "This combination of pieces does not belong to any drone category (Aérien, Marin, Terrestre, Submersible)";
            return false;
        }

        template = candidate;
        return true;
    }

    private bool TrySetOnce(ref string? slot, string piece, string slotLabel, out string? error)
    {
        if (slot is not null)
        {
            error = $"A template can only have one {slotLabel}, got both `{slot}` and `{piece}`";
            return false;
        }

        slot = piece;
        error = null;
        return true;
    }
}
