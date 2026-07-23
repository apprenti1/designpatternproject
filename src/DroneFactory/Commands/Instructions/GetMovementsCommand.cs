using DroneFactory.Storage;

namespace DroneFactory.Commands.Instructions;

/// <summary>
/// GET_MOVEMENTS (readme.md §5.2.3). Unlike the other commands, this one doesn't delegate to
/// <see cref="InstructionHandler"/>: its data source is the movement log fed by the
/// <see cref="LoggingInstruction"/> decorator, not the handler's business logic.
/// </summary>
public sealed class GetMovementsCommand : IInstruction
{
    private readonly IMovementRepository _movements;

    public GetMovementsCommand(IMovementRepository movements) => _movements = movements;

    public string Name => "GET_MOVEMENTS";

    public IEnumerable<string> Execute(string args)
    {
        var filter = string.IsNullOrWhiteSpace(args)
            ? null
            : args.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(LastToken)
                .ToHashSet();

        foreach (var movement in _movements.All)
        {
            if (filter is null || Mentions(movement.Args, filter))
            {
                yield return $"{movement.Instruction} {movement.Args}";
            }
        }
    }

    // ARGS entries may be bare item names ("Piece1") or quantified ("2 Piece1") — accept both.
    private static string LastToken(string entry)
        => entry.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last();

    private static bool Mentions(string args, HashSet<string> names)
        => args.Split(new[] { ' ', ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(names.Contains);
}
