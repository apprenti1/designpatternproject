namespace DroneFactory.Domain.Categories;

/// <summary>
/// Tag lookups shared by every rule. Only the hull, generator(s), movement module(s) and installed
/// system carry category-relevant tags (F/M/L/S) — the main and control modules never restrict
/// categorization (readme.md §4.2, "Les modules principaux et de contrôle ne restreignent jamais
/// la catégorisation"). See docs/HYPOTHESES.md for why this reading is required for the existing
/// catalog (§6.2) to be internally consistent, and for how multi-part slots (§5.1.2) are read:
/// a category needing "a movement module (X)" matches if ANY movement module carries tag X, while
/// Submersible's "every part is of type (S)" requires ALL generators and ALL movement modules to
/// carry tag S.
/// </summary>
internal static class PartTags
{
    public static IReadOnlyList<string> Hull(DroneTemplate template) => TagsOf(PieceCatalog.Hulls, template.Hull);

    public static IEnumerable<IReadOnlyList<string>> Generators(DroneTemplate template)
        => template.Generators.Select(g => TagsOf(PieceCatalog.Generators, g));

    public static IEnumerable<IReadOnlyList<string>> MovementModules(DroneTemplate template)
        => template.MovementModules.Select(m => TagsOf(PieceCatalog.MovementModules, m));

    public static IReadOnlyList<string> System(DroneTemplate template)
        => SystemCatalog.All.FirstOrDefault(s => s.Name == template.System)?.Tags ?? Array.Empty<string>();

    private static IReadOnlyList<string> TagsOf(IReadOnlyList<Piece> catalog, string pieceName)
        => catalog.FirstOrDefault(p => p.Name == pieceName)?.Tags ?? Array.Empty<string>();
}

public sealed class AerienRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Aerien;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.MovementModules(droneTemplate).Any(tags => tags.Contains("F")) && PartTags.System(droneTemplate).Contains("3D");
}

public sealed class MarinRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Marin;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.Hull(droneTemplate).Contains("S")
        && PartTags.System(droneTemplate).Contains("2D")
        && PartTags.MovementModules(droneTemplate).Any(tags => tags.Contains("M"));
}

public sealed class TerrestreRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Terrestre;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.MovementModules(droneTemplate).Any(tags => tags.Contains("L")) && PartTags.System(droneTemplate).Contains("2D");
}

public sealed class SubmersibleRule : ICategoryRule
{
    public DroneCategory Category => DroneCategory.Submersible;

    public bool Matches(DroneTemplate droneTemplate)
        => PartTags.Hull(droneTemplate).Contains("S")
        && PartTags.Generators(droneTemplate).All(tags => tags.Contains("S"))
        && PartTags.MovementModules(droneTemplate).All(tags => tags.Contains("S"))
        && PartTags.System(droneTemplate).Contains("3D");
}
