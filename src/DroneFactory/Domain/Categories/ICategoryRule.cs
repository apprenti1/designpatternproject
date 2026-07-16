namespace DroneFactory.Domain.Categories;

/// <summary>
/// A single category test (readme.md §4.2). One implementation per category so a new category
/// (e.g. a future MyGES module) can be added without touching the others — see
/// docs/DESIGN_PATTERNS.md for why this is a Strategy rather than a single switch.
/// </summary>
public interface ICategoryRule
{
    DroneCategory Category { get; }

    bool Matches(DroneTemplate droneTemplate);
}
