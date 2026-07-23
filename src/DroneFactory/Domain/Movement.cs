namespace DroneFactory.Domain;

/// <summary>
/// One stock-impacting instruction execution, recorded by the <c>LoggingInstruction</c> decorator
/// for GET_MOVEMENTS (readme.md §5.2.3).
/// </summary>
public sealed record Movement(DateTimeOffset Timestamp, string Instruction, string Args);
