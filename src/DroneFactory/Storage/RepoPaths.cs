namespace DroneFactory.Storage;

public static class RepoPaths
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DroneFactory.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (DroneFactory.sln not found).");
    }

    public static string DataDirectory => Path.Combine(FindRepoRoot(), "data");
}
