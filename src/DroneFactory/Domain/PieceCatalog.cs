namespace DroneFactory.Domain;

/// <summary>
/// The fixed catalog of base parts the factory works with (readme.md §6.2).
/// </summary>
public static class PieceCatalog
{
    public static readonly IReadOnlyList<Piece> Hulls = new[]
    {
        new Piece("Hull_HG1", new[] { "S" }),
        new Piece("Hull_HF1", Array.Empty<string>()),
        new Piece("Hull_HS1", new[] { "S" }),
    };

    public static readonly IReadOnlyList<Piece> MainModules = new[]
    {
        new Piece("Core_CG1", new[] { "2D" }),
        new Piece("Core_C3D1", new[] { "2D", "3D" }),
    };

    public static readonly IReadOnlyList<Piece> Generators = new[]
    {
        new Piece("Generator_GG1", Array.Empty<string>()),
        new Piece("Generator_GF1", Array.Empty<string>()),
        new Piece("Generator_GS1", new[] { "S" }),
    };

    public static readonly IReadOnlyList<Piece> MovementModules = new[]
    {
        new Piece("Move_MF1", new[] { "F" }),
        new Piece("Move_ML1", new[] { "L" }),
        new Piece("Move_MS1", new[] { "S" }),
        new Piece("Move_MM1", new[] { "M" }),
        new Piece("Move_MU1", new[] { "M", "L" }),
        new Piece("Move_MS2", new[] { "M", "S" }),
    };

    public static readonly IReadOnlyList<Piece> ControlModules = new[]
    {
        new Piece("Processor_PG1", new[] { "2D" }),
        new Piece("Processor_P3D1", new[] { "3D" }),
        new Piece("Processor_PU1", new[] { "2D", "3D" }),
    };

    public static IEnumerable<Piece> All =>
        Hulls.Concat(MainModules).Concat(Generators).Concat(MovementModules).Concat(ControlModules);
}
