using System.Text.Json;
using System.Text.Json.Serialization;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Loads the Swagger route documentation from the sibling EndpointDocs.json file, so the
/// French summaries/descriptions live in a plain data file rather than in C# source.
/// </summary>
public static class EndpointDocs
{
    private static readonly Lazy<EndpointDocsFile> LoadedDocs = new(Load);

    public static EndpointInfo Info => LoadedDocs.Value.Info;

    public static IReadOnlyDictionary<string, EndpointOperation> Operations => LoadedDocs.Value.Operations;

    private static EndpointDocsFile Load()
    {
        var path = Path.Combine(RepoPaths.FindRepoRoot(), "src", "DroneFactory", "Commands", "EndpointDocs.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EndpointDocsFile>(json)
            ?? throw new InvalidOperationException($"Could not parse {path}");
    }

    private sealed record EndpointDocsFile(
        [property: JsonPropertyName("info")] EndpointInfo Info,
        [property: JsonPropertyName("operations")] Dictionary<string, EndpointOperation> Operations);
}

public sealed record EndpointInfo(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string Description);

public sealed record EndpointOperation(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("description")] string Description);
