namespace DroneFactory.Domain;

/// <summary>
/// A drone template. Since readme.md §5.1.2, a drone can have 1-2 generators and 1-3 movement
/// modules (previously exactly one of each) — see <see cref="DroneTemplateBuilder"/> for the
/// construction-count rule this implies.
/// </summary>
public sealed record DroneTemplate(
    string Name,
    string Hull,
    string MainModule,
    IReadOnlyList<string> Generators,
    IReadOnlyList<string> MovementModules,
    string ControlModule,
    string System)
{
    /// <summary>Convenience accessor for the common single-generator case (tests, display).</summary>
    public string Generator => Generators[0];

    /// <summary>Convenience accessor for the common single-movement-module case (tests, display).</summary>
    public string MovementModule => MovementModules[0];

    public IEnumerable<string> RequiredPieces =>
        new[] { Hull, MainModule, ControlModule }.Concat(Generators).Concat(MovementModules);
}
