namespace DroneFactory.Domain.Categories;

/// <summary>
/// The four drone categories (readme.md §4.2). A drone can belong to several at once,
/// but never zero — <see cref="CategoryClassifier"/> is what enforces that.
/// </summary>
[Flags]
public enum DroneCategory
{
    None = 0,
    Aerien = 1,
    Marin = 2,
    Terrestre = 4,
    Submersible = 8,
}
