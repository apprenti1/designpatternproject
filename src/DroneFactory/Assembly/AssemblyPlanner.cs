using DroneFactory.Domain;

namespace DroneFactory.Assembly;

/// <summary>
/// Builds the internal assembly instruction sequence for one drone (readme.md §3.2.3),
/// respecting the four ordering constraints: parts out of stock before use, only the
/// generator(s) may join the hull before the main module, the movement module(s) join only
/// after the hull is already present, and systems are installed before their part is
/// used in any assembly. Generalized for §5.1.2 (up to 2 generators, up to 3 movement modules):
/// each one folds into the running assembly in turn, incrementing the TMP counter. See
/// docs/HYPOTHESES.md for why this deviates from the (internally inconsistent) worked example in
/// readme.md §7.1, and for the GET_OUT_STOCK aggregation-by-piece decision below.
/// </summary>
public static class AssemblyPlanner
{
    public static IEnumerable<string> BuildInstructions(DroneTemplate drone)
    {
        yield return $"PRODUCING {drone.Name}";

        var pullOrder = new List<string> { drone.Hull, drone.MainModule };
        pullOrder.AddRange(drone.Generators);
        pullOrder.AddRange(drone.MovementModules);
        pullOrder.Add(drone.ControlModule);

        // Count occurrences once (a drone can need e.g. 2 of the same movement module), then
        // emit one GET_OUT_STOCK per distinct piece, in first-use order.
        var countByPiece = pullOrder.GroupBy(piece => piece).ToDictionary(group => group.Key, group => group.Count());
        var pulled = new HashSet<string>();
        foreach (var piece in pullOrder)
        {
            if (pulled.Add(piece))
            {
                yield return $"GET_OUT_STOCK {countByPiece[piece]} {piece}";
            }
        }

        yield return $"INSTALL {drone.System} {drone.MainModule}";

        var counter = 0;
        var current = drone.Hull;

        // Only the generator(s) may be mounted into the hull before the main module.
        foreach (var generator in drone.Generators)
        {
            counter++;
            yield return $"ASSEMBLE TMP{counter} {current} {generator}";
            current = $"TMP{counter}";
        }

        // The main module (system already installed) joins next.
        counter++;
        yield return $"ASSEMBLE TMP{counter} {current} {drone.MainModule}{{{drone.System}}}";
        current = $"TMP{counter}";

        // The movement module(s) are assembled only after the hull is already present.
        foreach (var movementModule in drone.MovementModules)
        {
            counter++;
            yield return $"ASSEMBLE TMP{counter} {current} {movementModule}";
            current = $"TMP{counter}";
        }

        // Final assembly is left unnamed: it is a known drone template.
        yield return $"ASSEMBLE {current} {drone.ControlModule}";

        yield return $"FINISHED {drone.Name}";
    }

    public static IEnumerable<string> BuildInstructions(DroneTemplate drone, int quantity)
        => Enumerable.Range(0, quantity).SelectMany(_ => BuildInstructions(drone));
}
