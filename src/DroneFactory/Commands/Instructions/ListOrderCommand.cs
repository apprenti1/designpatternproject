namespace DroneFactory.Commands.Instructions;

public sealed class ListOrderCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public ListOrderCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "LIST_ORDER";

    public IEnumerable<string> Execute(string args) => _handler.ListOrder();
}
