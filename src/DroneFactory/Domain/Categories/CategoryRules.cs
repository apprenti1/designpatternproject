namespace DroneFactory.Domain.Categories;

/// <summary>
/// Tag lookups shared by every rule. Only the hull, generator, movement module and installed
/// system carry category-relevant tags (F/M/L/S) — the main and control modules never restrict
/// categorization (readme.md §4.2, "Les modules principaux et de contrôle ne restreignent jamais
/// la catégorisation"). See docs/HYPOTHESES.md for why this reading is required for the existing
/// catalog (§6.2) to be internally consistent.
/// </summary>
internal static class PartTags
{
    public static IReadOnlyList<string> Hull(DroneTemplate template) => TagsOf(PieceCatalog.Hulls, template.Hull);

    public static IReadOnlyList<string> Generator(DroneTemplate template) => TagsOf(PieceCatalog.Generators, template.Generator);

    public static IReadOnlyList<string> MovementModule(DroneTemplate template) => TagsOf(PieceCatalog.MovementModules, template.MovementModule);

    public static IReadOnlyList<string> System(DroneTemplate template)
        => SystemCatalog.All.FirstOrDefault(s => s.Name == template.System)?.Tags ?? Array.Empty<string>();

    private static IReadOnlyList<string> TagsOf(IReadOnlyList<Piece> catalog, string pieceName)
        => catalog.FirstOrDefault(p => p.Name == pieceName)?.Tags ?? Array.Empty<string>();
}

public sealed class AerienRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Aerien;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.MovementModule(droneTemplate).Contains("F") && PartTags.System(droneTemplate).Contains("3D");
}

public sealed class MarinRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Marin;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.Hull(droneTemplate).Contains("S")
        && PartTags.System(droneTemplate).Contains("2D")
        && PartTags.MovementModule(droneTemplate).Contains("M");
}

public sealed class TerrestreRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Terrestre;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.MovementModule(droneTemplate).Contains("L") && PartTags.System(droneTemplate).Contains("2D");
}

public sealed class SubmersibleRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Submersible;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.Hull(droneTemplate).Contains("S")
        && PartTags.Generator(droneTemplate).Contains("S")
        && PartTags.MovementModule(droneTemplate).Contains("S")
        && PartTags.System(droneTemplate).Contains("3D");
}
