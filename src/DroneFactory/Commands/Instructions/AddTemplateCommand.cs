namespace DroneFactory.Commands.Instructions;

public sealed class AddTemplateCommand : IInstruction
{
    private readonly InstructionHandler _handler;

    public AddTemplateCommand(InstructionHandler handler) => _handler = handler;

    public string Name => "ADD_TEMPLATE";

    public IEnumerable<string> Execute(string args) => _handler.AddTemplate(args);
}
