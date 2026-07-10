namespace DroneFactory.Domain;

public sealed record DroneTemplate(
    string Name,
    string Hull,
    string MainModule,
    string Generator,
    string MovementModule,
    string ControlModule,
    string System)
{
    public IEnumerable<string> RequiredPieces => new[] { Hull, MainModule, Generator, MovementModule, ControlModule };
}
