namespace DroneFactory.Commands.Instructions;

public sealed class StocksCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public StocksCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "STOCKS";

    public IEnumerable<string> Execute(string args) => _handler.Stocks(args);
}
