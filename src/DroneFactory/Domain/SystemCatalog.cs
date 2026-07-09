namespace DroneFactory.Domain;

/// <summary>
/// The fixed catalog of main systems the factory can install (readme.md §6.2).
/// </summary>
public static class SystemCatalog
{
    public static readonly IReadOnlyList<SystemPart> All = new[]
    {
        new SystemPart("System_SG1", new[] { "2D" }),
        new SystemPart("System_S3D1", new[] { "2D", "3D" }),
    };
}
