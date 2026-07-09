namespace DroneFactory.Domain;

public sealed record SystemPart(string Name, IReadOnlyList<string> Tags);
