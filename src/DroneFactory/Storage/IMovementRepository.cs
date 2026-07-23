using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// Persistence boundary for the stock movement log (GET_MOVEMENTS, readme.md §5.2.3), fed by the
/// <c>LoggingInstruction</c> decorator rather than by <c>InstructionHandler</c> directly.
/// </summary>
public interface IMovementRepository
{
    IReadOnlyList<Movement> All { get; }

    void Record(string instruction, string args);
}
