namespace DroneFactory.Storage;

public static class RepoPaths
{
    // Cached after the first call: the repo root never changes during the process's lifetime,
    // and every store (StockStore, TemplateStore, OrderStore, MovementStore, ...) resolves it
    // independently at startup, so walking the directory tree each time would repeat the same
    // filesystem scan several times over.
    private static string? _cachedRepoRoot;

    public static string FindRepoRoot()
    {
        if (_cachedRepoRoot is not null)
        {
            return _cachedRepoRoot;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DroneFactory.sln")))
        {
            dir = dir.Parent;
        }

        _cachedRepoRoot = dir?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (DroneFactory.sln not found).");
        return _cachedRepoRoot;
    }

    public static string DataDirectory => Path.Combine(FindRepoRoot(), "data");
}
