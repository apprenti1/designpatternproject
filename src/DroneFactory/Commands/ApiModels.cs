namespace DroneFactory.Commands;

public sealed record ArgsRequest(string Args);

public sealed record LinesResponse(string[] Lines);

/// <summary>Read-only convenience for the front-end — not one of the subject's instructions.</summary>
public sealed record TemplateInfo(
    string Name,
    string[] Categories,
    string Hull,
    string MainModule,
    string[] Generators,
    string[] MovementModules,
    string ControlModule,
    string System);
