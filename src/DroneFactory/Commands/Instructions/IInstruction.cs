namespace DroneFactory.Commands.Instructions;

/// <summary>
/// One user-facing instruction (readme.md §6.1). Each implementation is a thin adapter onto
/// <see cref="InstructionHandler"/>, which keeps the actual business logic and its existing test
/// coverage — this layer only exists so dispatch (console/API) is table-driven instead of a
/// growing switch statement. See docs/DESIGN_PATTERNS.md ("Command").
/// </summary>
public interface IInstruction
{
    string Name { get; }

    IEnumerable<string> Execute(string args);
}
