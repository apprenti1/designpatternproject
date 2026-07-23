using DroneFactory.Domain.Categories;

namespace DroneFactory.Domain;

/// <summary>
/// Classifies a flat list of piece/system names into a validated <see cref="DroneTemplate"/>
/// (readme.md §4.3, §5.1.2). Shared by ADD_TEMPLATE and drone-modifier resolution (WITH/WITHOUT/
/// REPLACE, §5.2.1) so both paths enforce the same slot, compatibility, construction-count and
/// category rules instead of duplicating them.
/// </summary>
public static class DroneTemplateBuilder
{
    public const int MaxGenerators = 2;
    public const int MaxMovementModules = 3;

    public static bool TryBuild(string name, IEnumerable<string> pieceOrSystemNames, out DroneTemplate? template, out string? error)
    {
        template = null;
        error = null;

        string? hull = null;
        string? mainModule = null;
        string? controlModule = null;
        string? system = null;
        var generators = new List<string>();
        var movementModules = new List<string>();

        foreach (var piece in pieceOrSystemNames)
        {
            if (PieceCatalog.Hulls.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref hull, piece, "hull", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.MainModules.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref mainModule, piece, "main module", out error))
                {
                    return false;
                }
            }
            else if (PieceCatalog.Generators.Any(p => p.Name == piece))
            {
                generators.Add(piece);
            }
            else if (PieceCatalog.MovementModules.Any(p => p.Name == piece))
            {
                movementModules.Add(piece);
            }
            else if (PieceCatalog.ControlModules.Any(p => p.Name == piece))
            {
                if (!TrySetOnce(ref controlModule, piece, "control module", out error))
                {
                    return false;
                }
            }
            else if (SystemCatalog.All.Any(s => s.Name == piece))
            {
                if (!TrySetOnce(ref system, piece, "system", out error))
                {
                    return false;
                }
            }
            else
            {
                error = $"`{piece}` is not a recognized piece or system";
                return false;
            }
        }

        if (hull is null || mainModule is null || controlModule is null || system is null || generators.Count == 0 || movementModules.Count == 0)
        {
            error = "A template requires exactly one hull, one main module, one to two generators, one to three movement modules, one control module and one system";
            return false;
        }

        if (generators.Count > MaxGenerators)
        {
            error = $"A template can have at most {MaxGenerators} generators, got {generators.Count}";
            return false;
        }

        if (movementModules.Count > MaxMovementModules)
        {
            error = $"A template can have at most {MaxMovementModules} movement modules, got {movementModules.Count}";
            return false;
        }

        if (movementModules.Count >= 2 && generators.Count != 2)
        {
            error = "A template with 2 or more movement modules must have exactly 2 generators";
            return false;
        }

        var mainModuleTags = PieceCatalog.MainModules.First(p => p.Name == mainModule).Tags;
        var controlModuleTags = PieceCatalog.ControlModules.First(p => p.Name == controlModule).Tags;
        var systemTags = SystemCatalog.All.First(s => s.Name == system).Tags;

        if (!systemTags.All(mainModuleTags.Contains))
        {
            error = $"Main module `{mainModule}` does not support system `{system}`";
            return false;
        }

        if (!controlModuleTags.Intersect(systemTags).Any())
        {
            error = $"Control module `{controlModule}` is not compatible with system `{system}`";
            return false;
        }

        var candidate = new DroneTemplate(name, hull, mainModule, generators, movementModules, controlModule, system);
        if (CategoryClassifier.Classify(candidate) == DroneCategory.None)
        {
            error = "This combination of pieces does not belong to any drone category (Aérien, Marin, Terrestre, Submersible)";
            return false;
        }

        template = candidate;
        return true;
    }

    private static bool TrySetOnce(ref string? slot, string piece, string slotLabel, out string? error)
    {
        if (slot is not null)
        {
            error = $"A template can only have one {slotLabel}, got both `{slot}` and `{piece}`";
            return false;
        }

        slot = piece;
        error = null;
        return true;
    }
}
