namespace DroneFactory.Commands.Instructions;

public sealed class AssemblyInstructionsCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public AssemblyInstructionsCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "INSTRUCTIONS";

    public IEnumerable<string> Execute(string args) => _handler.Instructions(args);
}
