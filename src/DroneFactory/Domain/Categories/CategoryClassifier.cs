namespace DroneFactory.Domain.Categories;

/// <summary>
/// Applies every <see cref="ICategoryRule"/> to a template and combines the matches. A drone
/// belonging to zero categories is invalid (readme.md §4.2) — callers (e.g. ADD_TEMPLATE
/// validation) are expected to reject a <see cref="DroneCategory.None"/> result.
/// </summary>
public static class CategoryClassifier
{
    private static readonly IReadOnlyList<ICategoryRule> Rules = new ICategoryRule[]
    {
        new AerienRule(),
        new MarinRule(),
        new TerrestreRule(),
        new SubmersibleRule(),
    };

    public static DroneCategory Classify(DroneTemplate template)
    {
        var result = DroneCategory.None;
        foreach (var rule in Rules)
        {
            if (rule.Matches(template))
            {
                result |= rule.Category;
            }
        }

        return result;
    }

    public static IEnumerable<string> Names(DroneCategory categories)
        => Enum.GetValues<DroneCategory>()
            .Where(c => c != DroneCategory.None && categories.HasFlag(c))
            .Select(c => c.ToString());
}
