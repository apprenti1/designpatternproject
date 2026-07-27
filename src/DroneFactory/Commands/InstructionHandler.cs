using DroneFactory.Assembly;
using DroneFactory.Domain;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Pure business logic for the user instructions (readme.md §3.2, §4.3, §5). Each method
/// returns the output lines to print, so it can be exercised in tests without touching Console.
/// </summary>
public sealed class InstructionHandler
{
    private readonly IFactoryRegistry _factories;
    private readonly ITemplateRepository _templates;
    private readonly IOrderRepository _orders;

    /// <summary>
    /// Legacy single-factory constructor, kept so the phase 1/2 test suite (and any caller that
    /// doesn't care about multi-factory) doesn't need to know about <see cref="IFactoryRegistry"/>.
    /// Wraps <paramref name="stock"/> as the sole factory "Usine1" — with a single factory, every
    /// IN-qualified code path resolves it implicitly and no ambiguity error can ever trigger.
    /// </summary>
    public InstructionHandler(IStockRepository stock, ITemplateRepository templates)
        : this(new FactoryStore(new Dictionary<string, IStockRepository> { ["Usine1"] = stock }), templates, new OrderStore())
    {
    }

    public InstructionHandler(IFactoryRegistry factories, ITemplateRepository templates, IOrderRepository orders)
    {
        _factories = factories;
        _templates = templates;
        _orders = orders;
    }

    public IEnumerable<string> Stocks() => Stocks(string.Empty);

    public IEnumerable<string> Stocks(string args)
    {
        var (_, factoryName) = FactoryQualifier.Extract(args);

        if (factoryName is not null)
        {
            if (!_factories.Exists(factoryName))
            {
                yield return $"ERROR Unknown factory `{factoryName}`";
                yield break;
            }

            var stock = _factories.GetStock(factoryName);
            foreach (var drone in _templates.All)
            {
                yield return $"{stock.GetQuantity(drone.Name)} {drone.Name}";
            }

            foreach (var piece in PieceCatalog.All)
            {
                yield return $"{stock.GetQuantity(piece.Name)} {piece.Name}";
            }

            yield break;
        }

        // No IN precision: aggregate across every factory (readme.md §5.2.4).
        foreach (var drone in _templates.All)
        {
            yield return $"{_factories.TotalQuantity(drone.Name)} {drone.Name}";
        }

        foreach (var piece in PieceCatalog.All)
        {
            yield return $"{_factories.TotalQuantity(piece.Name)} {piece.Name}";
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

        foreach (var (droneName, quantity, drone) in order)
        {
            yield return $"{quantity} {droneName} :";

            var perDrone = new Dictionary<string, int>();
            foreach (var piece in drone.RequiredPieces)
            {
                perDrone[piece] = perDrone.GetValueOrDefault(piece) + 1;
            }

            foreach (var (piece, unitCount) in perDrone)
            {
                var lineQuantity = unitCount * quantity;
                yield return $"{lineQuantity} {piece}";
                total[piece] = total.GetValueOrDefault(piece) + lineQuantity;
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

        foreach (var (_, quantity, drone) in order)
        {
            foreach (var line in AssemblyPlanner.BuildInstructions(drone, quantity))
            {
                yield return line;
            }
        }
    }

    public IEnumerable<string> Verify(string args)
    {
        var (rest, factoryName) = FactoryQualifier.Extract(args);

        if (!TryParseOrder(rest, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        var needed = AggregateNeededPieces(order);

        if (!TryResolveFactory(factoryName, out var stock, out var factoryError))
        {
            yield return $"ERROR {factoryError}";
            yield break;
        }

        yield return stock!.HasAtLeast(needed) ? "AVAILABLE" : "UNAVAILABLE";
    }

    public IEnumerable<string> Produce(string args)
    {
        var (rest, factoryName) = FactoryQualifier.Extract(args);

        if (!TryParseOrder(rest, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        var needed = AggregateNeededPieces(order);

        // Only PRODUCE's missing-factory error is stock-sufficiency-filtered per the
        // readme.md §5.2.4 worked example; other instructions list every factory (see
        // TryResolveFactory / docs/HYPOTHESES.md), so the filter is passed in just for this call.
        if (!TryResolveFactory(
                factoryName,
                out var stock,
                out var factoryError,
                candidateFilter: f => _factories.GetStock(f).HasAtLeast(needed),
                noCandidatesError: "Insufficient stock to produce this order in any factory"))
        {
            yield return $"ERROR {factoryError}";
            yield break;
        }

        if (!stock!.HasAtLeast(needed))
        {
            yield return "ERROR Insufficient stock to produce this order";
            yield break;
        }

        stock.Consume(needed);
        foreach (var (droneName, quantity, _) in order)
        {
            stock.Add(droneName, quantity);
        }

        stock.Save();
        yield return "STOCK_UPDATED";
    }

    public IEnumerable<string> Receive(string args)
    {
        var (rest, factoryName) = FactoryQualifier.Extract(args);

        if (!ArgsParser.TryParse(rest, out var items, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        if (!TryValidateKnownItems(items, out var itemsError))
        {
            yield return $"ERROR {itemsError}";
            yield break;
        }

        if (!TryResolveFactory(factoryName, out var stock, out var factoryError))
        {
            yield return $"ERROR {factoryError}";
            yield break;
        }

        foreach (var (name, quantity) in items)
        {
            stock!.Add(name, quantity);
        }

        stock!.Save();
        yield return "STOCK_UPDATED";
    }

    public IEnumerable<string> Transfer(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            yield return "ERROR Missing arguments";
            yield break;
        }

        var tokens = args.Split(',', 3, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3)
        {
            yield return "ERROR Expected format 'TRANSFER Usine1, Usine2, ARGS'";
            yield break;
        }

        var fromName = tokens[0];
        var toName = tokens[1];

        if (!_factories.Exists(fromName))
        {
            yield return $"ERROR Unknown factory `{fromName}`";
            yield break;
        }

        if (!_factories.Exists(toName))
        {
            yield return $"ERROR Unknown factory `{toName}`";
            yield break;
        }

        if (fromName == toName)
        {
            yield return "ERROR Source and destination factory must be different";
            yield break;
        }

        if (!ArgsParser.TryParse(tokens[2], out var items, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        if (!TryValidateKnownItems(items, out var itemsError))
        {
            yield return $"ERROR {itemsError}";
            yield break;
        }

        var from = _factories.GetStock(fromName);
        var to = _factories.GetStock(toName);

        if (!from.HasAtLeast(items))
        {
            yield return "ERROR Insufficient stock to transfer";
            yield break;
        }

        from.Consume(items);
        foreach (var (name, quantity) in items)
        {
            to.Add(name, quantity);
        }

        from.Save();
        to.Save();
        yield return "STOCK_UPDATED";
    }

    public IEnumerable<string> Order(string args)
    {
        if (!TryParseOrder(args, out var order, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        var lines = new Dictionary<string, int>();
        foreach (var (name, quantity, _) in order)
        {
            lines[name] = lines.GetValueOrDefault(name) + quantity;
        }

        yield return _orders.Create(lines);
    }

    public IEnumerable<string> Send(string args)
    {
        var commaIndex = args.IndexOf(',');
        if (commaIndex < 0)
        {
            yield return "ERROR Expected format 'SEND ORDERID, ARGS'";
            yield break;
        }

        var orderId = args[..commaIndex].Trim();
        var rest = args[(commaIndex + 1)..].Trim();

        var order = _orders.Find(orderId);
        if (order is null)
        {
            yield return $"ERROR `{orderId}` is not a recognized order";
            yield break;
        }

        var (itemsArgs, factoryName) = FactoryQualifier.Extract(rest);

        if (!ArgsParser.TryParse(itemsArgs, out var toSend, out var error))
        {
            yield return $"ERROR {error}";
            yield break;
        }

        foreach (var (name, quantity) in toSend)
        {
            var remaining = order.Remaining.GetValueOrDefault(name);
            if (quantity > remaining)
            {
                yield return $"ERROR Cannot send {quantity} {name}: only {remaining} remaining for `{orderId}`";
                yield break;
            }
        }

        if (!TryResolveFactory(factoryName, out var stock, out var factoryError))
        {
            yield return $"ERROR {factoryError}";
            yield break;
        }

        if (!stock!.HasAtLeast(toSend))
        {
            yield return "ERROR Insufficient stock to send this order";
            yield break;
        }

        stock.Consume(toSend);
        stock.Save();

        var newRemaining = new Dictionary<string, int>(order.Remaining);
        foreach (var (name, quantity) in toSend)
        {
            var left = newRemaining[name] - quantity;
            if (left <= 0)
            {
                newRemaining.Remove(name);
            }
            else
            {
                newRemaining[name] = left;
            }
        }

        if (newRemaining.Count == 0)
        {
            _orders.Remove(orderId);
            yield return $"COMPLETED {orderId}";
            yield break;
        }

        _orders.UpdateRemaining(orderId, newRemaining);
        yield return $"Remaining for {orderId} : {FormatArgs(newRemaining)}";
    }

    public IEnumerable<string> ListOrder()
    {
        foreach (var order in _orders.All)
        {
            yield return $"{order.Id}: {FormatArgs(order.Remaining)}";
        }
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

    private static string FormatArgs(IReadOnlyDictionary<string, int> items)
        => string.Join(", ", items.Select(kv => $"{kv.Value} {kv.Key}"));

    private static string JoinWithAnd(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            return string.Empty;
        }

        if (names.Count == 1)
        {
            return names[0];
        }

        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
    }

    /// <summary>
    /// Resolves an explicit "IN Usine1" qualifier, or the sole factory when there is only one,
    /// or fails with the "missing target factory" error listing the candidates. Most callers list
    /// every factory as candidates; <paramref name="candidateFilter"/> lets PRODUCE narrow that
    /// list to factories with enough stock instead (readme.md §5.2.4), with its own error message
    /// via <paramref name="noCandidatesError"/> when the filtered list is empty.
    /// </summary>
    private bool TryResolveFactory(
        string? factoryName,
        out IStockRepository? stock,
        out string? error,
        Func<string, bool>? candidateFilter = null,
        string? noCandidatesError = null)
    {
        if (factoryName is not null)
        {
            if (!_factories.Exists(factoryName))
            {
                stock = null;
                error = $"Unknown factory `{factoryName}`";
                return false;
            }

            stock = _factories.GetStock(factoryName);
            error = null;
            return true;
        }

        if (_factories.Names.Count == 1)
        {
            stock = _factories.GetStock(_factories.Names[0]);
            error = null;
            return true;
        }

        stock = null;

        var candidates = candidateFilter is null ? _factories.Names : _factories.Names.Where(candidateFilter).ToList();
        if (candidateFilter is not null && candidates.Count == 0)
        {
            error = noCandidatesError;
            return false;
        }

        error = $"Missing target factory. Available factory for this instruction are {JoinWithAnd(candidates)}";
        return false;
    }

    private bool IsKnownStockItem(string name)
        => PieceCatalog.All.Any(p => p.Name == name)
        || SystemCatalog.All.Any(s => s.Name == name)
        || _templates.Find(name) is not null;

    /// <summary>
    /// Shared by <see cref="Receive"/> and <see cref="Transfer"/>: every item name in a stock
    /// movement must be a recognized piece, system or drone template.
    /// </summary>
    private bool TryValidateKnownItems(IReadOnlyDictionary<string, int> items, out string? error)
    {
        foreach (var name in items.Keys)
        {
            if (!IsKnownStockItem(name))
            {
                error = $"`{name}` is not a recognized piece, system or drone";
                return false;
            }
        }

        error = null;
        return true;
    }

    private bool TryParseOrder(string args, out List<(string Name, int Quantity, DroneTemplate Template)> order, out string? error)
        => DroneOrderParser.TryParse(args, _templates, out order, out error);

    private Dictionary<string, int> AggregateNeededPieces(List<(string Name, int Quantity, DroneTemplate Template)> order)
    {
        var needed = new Dictionary<string, int>();
        foreach (var (_, quantity, drone) in order)
        {
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

        return DroneTemplateBuilder.TryBuild(name, tokens.Skip(1), out template, out error);
    }
}
