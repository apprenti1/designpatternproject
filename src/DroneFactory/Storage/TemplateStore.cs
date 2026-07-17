using System.Text.Json;
using DroneFactory.Domain;

namespace DroneFactory.Storage;

/// <summary>
/// Loads the fixed catalog (<see cref="DroneCatalog"/>) plus any custom templates registered via
/// ADD_TEMPLATE, persisted in data/templates.json (only the custom ones — the built-in catalog
/// never needs rewriting). Same seed/live split spirit as <see cref="StockStore"/>, except here
/// there is no seed file: the "seed" is the hardcoded catalog itself.
/// </summary>
public sealed class TemplateStore : ITemplateRepository
{
    private readonly string _customTemplatesPath;
    private readonly Dictionary<string, DroneTemplate> _templates;

    public TemplateStore(string dataDirectory)
    {
        _customTemplatesPath = Path.Combine(dataDirectory, "templates.json");
        _templates = DroneCatalog.All.ToDictionary(t => t.Name);

        if (File.Exists(_customTemplatesPath))
        {
            var json = File.ReadAllText(_customTemplatesPath);
            var custom = JsonSerializer.Deserialize<List<DroneTemplate>>(json) ?? new List<DroneTemplate>();
            foreach (var template in custom)
            {
                _templates[template.Name] = template;
            }
        }
    }

    public static TemplateStore CreateDefault() => new(RepoPaths.DataDirectory);

    public IReadOnlyList<DroneTemplate> All => _templates.Values.ToList();

    public DroneTemplate? Find(string name) => _templates.GetValueOrDefault(name);

    public void Add(DroneTemplate droneTemplate)
    {
        _templates[droneTemplate.Name] = droneTemplate;

        var custom = _templates.Values.Where(t => DroneCatalog.Find(t.Name) is null).ToList();
        var json = JsonSerializer.Serialize(custom, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_customTemplatesPath, json);
    }
}
