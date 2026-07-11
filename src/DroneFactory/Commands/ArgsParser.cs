namespace DroneFactory.Commands;

/// <summary>
/// Parses the quantified drone list shared by every user instruction (readme.md §3.1),
/// e.g. "1 Drone1, 2 Drone2, 3 Drone1" -&gt; { Drone1: 4, Drone2: 2 }.
/// </summary>
public static class ArgsParser
{
    public static bool TryParse(string args, out Dictionary<string, int> quantities, out string? error)
    {
        quantities = new Dictionary<string, int>();
        error = null;

        if (string.IsNullOrWhiteSpace(args))
        {
            error = "Missing arguments";
            return false;
        }

        foreach (var rawEntry in args.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawEntry.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var quantity) || quantity <= 0)
            {
                error = $"Invalid entry '{rawEntry}', expected format '<quantity> <DroneName>'";
                quantities = new Dictionary<string, int>();
                return false;
            }

            var name = parts[1];
            quantities[name] = quantities.GetValueOrDefault(name) + quantity;
        }

        return true;
    }
}
