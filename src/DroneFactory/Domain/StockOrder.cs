namespace DroneFactory.Domain;

/// <summary>
/// A backorder opened with ORDER and fulfilled incrementally with SEND (readme.md §5.2.2).
/// <see cref="Remaining"/> maps drone name to the quantity still owed.
/// </summary>
public sealed record StockOrder(string Id, Dictionary<string, int> Remaining);
