namespace DroneFactory.Commands.Instructions;

public sealed class ProduceCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public ProduceCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "PRODUCE";

    public IEnumerable<string> Execute(string args) => _handler.Produce(args);
}
