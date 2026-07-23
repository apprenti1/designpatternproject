namespace DroneFactory.Domain;

/// <summary>
/// The fixed catalog of drones the factory currently knows how to produce (readme.md §6.2).
/// </summary>
public static class DroneCatalog
{
    public static readonly IReadOnlyList<DroneTemplate> All = new[]
    {
        new DroneTemplate("DXF-1", "Hull_HF1", "Core_C3D1", new[] { "Generator_GF1" }, new[] { "Move_MF1" }, "Processor_P3D1", "System_S3D1"),
        new DroneTemplate("RDL-1", "Hull_HG1", "Core_CG1", new[] { "Generator_GG1" }, new[] { "Move_ML1" }, "Processor_PG1", "System_SG1"),
        new DroneTemplate("WDS-1", "Hull_HS1", "Core_C3D1", new[] { "Generator_GS1" }, new[] { "Move_MS1" }, "Processor_P3D1", "System_S3D1"),
        new DroneTemplate("DYM-1", "Hull_HG1", "Core_CG1", new[] { "Generator_GG1" }, new[] { "Move_MM1" }, "Processor_PG1", "System_SG1"),
    };

    public static DroneTemplate? Find(string name) => All.FirstOrDefault(d => d.Name == name);
}
