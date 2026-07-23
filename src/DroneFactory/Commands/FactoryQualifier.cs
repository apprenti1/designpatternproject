using System.Text.RegularExpressions;

namespace DroneFactory.Commands;

/// <summary>
/// Strips a trailing <c>IN Usine1</c> qualifier (readme.md §5.2.4) from an instruction's ARGS,
/// e.g. <c>"1 DXF-1 IN Usine1"</c> -&gt; <c>("1 DXF-1", "Usine1")</c>. Absent qualifier -&gt;
/// <c>(args, null)</c>, meaning "let the caller decide" (single factory: use it implicitly;
/// several factories: ambiguous, see <c>InstructionHandler</c>).
/// </summary>
public static class FactoryQualifier
{
    private static readonly Regex Pattern = new(@"\bIN\s+(\S+)\s*$", RegexOptions.Compiled);

    public static (string Remainder, string? Factory) Extract(string? args)
    {
        args ??= string.Empty;
        var match = Pattern.Match(args);
        if (!match.Success)
        {
            return (args, null);
        }

        var rest = args[..match.Index].TrimEnd().TrimEnd(',').Trim();
        return (rest, match.Groups[1].Value);
    }
}
