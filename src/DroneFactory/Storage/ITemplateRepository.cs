using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// Persistence boundary for drone templates: the fixed catalog (readme.md §6.2) plus whatever
/// was registered at runtime via ADD_TEMPLATE (§4.3). See <see cref="IStockRepository"/> for why
/// this is an interface (testability, multi-factory groundwork).
/// </summary>
public interface ITemplateRepository
{
    IReadOnlyList<DroneTemplate> All { get; }

    DroneTemplate? Find(string name);

    void Add(DroneTemplate droneTemplate);
}
