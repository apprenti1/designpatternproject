using DroneFactory.Domain;

namespace DroneFactory.Assembly;

/// <summary>
/// Builds the internal assembly instruction sequence for one drone (readme.md §3.2.3),
/// respecting the four ordering constraints: parts out of stock before use, only the
/// generator may join the hull before the main module, the movement module joins only
/// after the hull is already present, and systems are installed before their part is
/// used in any assembly. See docs/HYPOTHESES.md for why this deviates from the (internally
/// inconsistent) worked example in readme.md §7.1.
/// </summary>
public static class AssemblyPlanner
{
    public static IEnumerable<string> BuildInstructions(DroneTemplate drone)
    {
        yield return $"PRODUCING {drone.Name}";

        yield return $"GET_OUT_STOCK 1 {drone.Hull}";
        yield return $"GET_OUT_STOCK 1 {drone.MainModule}";
        yield return $"GET_OUT_STOCK 1 {drone.Generator}";
        yield return $"GET_OUT_STOCK 1 {drone.MovementModule}";
        yield return $"GET_OUT_STOCK 1 {drone.ControlModule}";

        yield return $"INSTALL {drone.System} {drone.MainModule}";

        // Only the generator may be mounted into the hull before the main module.
        yield return $"ASSEMBLE TMP1 {drone.Hull} {drone.Generator}";

        // The main module (system already installed) joins next.
        yield return $"ASSEMBLE TMP2 TMP1 {drone.MainModule}{{{drone.System}}}";

        // The movement module is assembled only after the hull is already present.
        yield return $"ASSEMBLE TMP3 TMP2 {drone.MovementModule}";

        // Final assembly is left unnamed: it is a known drone template.
        yield return $"ASSEMBLE TMP3 {drone.ControlModule}";

        yield return $"FINISHED {drone.Name}";
    }

    public static IEnumerable<string> BuildInstructions(DroneTemplate drone, int quantity)
        => Enumerable.Range(0, quantity).SelectMany(_ => BuildInstructions(drone));
}
