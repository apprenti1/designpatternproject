using DroneFactory.Commands.Instructions;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Table-driven dispatch for every user instruction (readme.md §6.1), built once from an
/// <see cref="InstructionHandler"/>. See docs/DESIGN_PATTERNS.md ("Command") for why this
/// replaces a switch statement, and ("Decorator") for why RECEIVE/PRODUCE/SEND/TRANSFER are
/// wrapped in <see cref="LoggingInstruction"/> here rather than inside the commands themselves.
/// </summary>
public sealed class InstructionRegistry
{
    private readonly Dictionary<string, IInstruction> _instructions;

    public InstructionRegistry(InstructionHandler handler, IMovementRepository movements)
    {
        IInstruction[] instructions =
        {
            new StocksCommand(handler),
            new NeededStocksCommand(handler),
            new AssemblyInstructionsCommand(handler),
            new VerifyCommand(handler),
            Logged(new ProduceCommand(handler), movements),
            new AddTemplateCommand(handler),
            Logged(new ReceiveCommand(handler), movements),
            new OrderCommand(handler),
            Logged(new SendCommand(handler), movements),
            new ListOrderCommand(handler),
            new GetMovementsCommand(movements),
            Logged(new TransferCommand(handler), movements),
        };

        _instructions = instructions.ToDictionary(i => i.Name);
    }

    public IEnumerable<string> Names => _instructions.Keys;

    public bool TryGet(string name, out IInstruction instruction) => _instructions.TryGetValue(name, out instruction!);

    private static IInstruction Logged(IInstruction inner, IMovementRepository movements) => new LoggingInstruction(inner, movements);
}
