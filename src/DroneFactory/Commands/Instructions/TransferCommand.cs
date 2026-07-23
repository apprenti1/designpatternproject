namespace DroneFactory.Commands.Instructions;

public sealed class TransferCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public TransferCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "TRANSFER";

    public IEnumerable<string> Execute(string args) => _handler.Transfer(args);
}
