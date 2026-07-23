namespace DroneFactory.Commands.Instructions;

public sealed class ReceiveCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public ReceiveCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "RECEIVE";

    public IEnumerable<string> Execute(string args) => _handler.Receive(args);
}
