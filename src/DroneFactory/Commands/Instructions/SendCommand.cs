namespace DroneFactory.Commands.Instructions;

public sealed class SendCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public SendCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "SEND";

    public IEnumerable<string> Execute(string args) => _handler.Send(args);
}
