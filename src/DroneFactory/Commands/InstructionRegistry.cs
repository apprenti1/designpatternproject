using DroneFactory.Commands.Instructions;

namespace DroneFactory.Commands;

/// <summary>
/// Table-driven dispatch for every user instruction (readme.md §6.1), built once from an
/// <see cref="InstructionHandler"/>. See docs/DESIGN_PATTERNS.md ("Command") for why this
/// replaces a switch statement.
/// </summary>
public sealed class InstructionRegistry
{
    private readonly Dictionary<string, IInstruction> _instructions;

    public InstructionRegistry(InstructionHandler handler)
    {
        IInstruction[] instructions =
        {
            new StocksCommand(handler),
            new NeededStocksCommand(handler),
            new AssemblyInstructionsCommand(handler),
            new VerifyCommand(handler),
            new ProduceCommand(handler),
            new AddTemplateCommand(handler),
        };

        _instructions = instructions.ToDictionary(i => i.Name);
    }

    public IEnumerable<string> Names => _instructions.Keys;

    public bool TryGet(string name, out IInstruction instruction) => _instructions.TryGetValue(name, out instruction!);
}
