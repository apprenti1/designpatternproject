using DroneFactory.Storage;

namespace DroneFactory.Commands.Instructions;

/// <summary>
/// Decorator (readme.md §5.2.3, GET_MOVEMENTS): wraps any stock-impacting <see cref="IInstruction"/>
/// and records a movement entry whenever it succeeds, without changing the wrapped command itself.
/// This is exactly the extension point anticipated for the Command pattern in
/// docs/DESIGN_PATTERNS.md — only <c>InstructionRegistry</c> needs to know which commands to wrap.
/// </summary>
public sealed class LoggingInstruction : IInstruction
{
    private readonly IInstruction _inner;
    private readonly IMovementRepository _movements;

    public LoggingInstruction(IInstruction inner, IMovementRepository movements)
    {
        _inner = inner;
        _movements = movements;
    }

    public string Name => _inner.Name;

    public IEnumerable<string> Execute(string args)
    {
        var lines = _inner.Execute(args).ToList();
        if (lines.Count > 0 && !lines[0].StartsWith("ERROR", StringComparison.Ordinal))
        {
            _movements.Record(_inner.Name, args);
        }

        return lines;
    }
}
