namespace DroneFactory.Commands.Instructions;

public sealed class OrderCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public OrderCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "ORDER";

    public IEnumerable<string> Execute(string args) => _handler.Order(args);
}
