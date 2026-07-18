namespace DroneFactory.Commands.Instructions;

public sealed class VerifyCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public VerifyCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "VERIFY";

    public IEnumerable<string> Execute(string args) => _handler.Verify(args);
}
