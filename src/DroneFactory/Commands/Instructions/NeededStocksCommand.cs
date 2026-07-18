namespace DroneFactory.Commands.Instructions;

public sealed class NeededStocksCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public NeededStocksCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "NEEDED_STOCKS";

    public IEnumerable<string> Execute(string args) => _handler.NeededStocks(args);
}
