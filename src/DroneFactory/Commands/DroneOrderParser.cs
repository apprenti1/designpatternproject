using System.Text.RegularExpressions;
using DroneFactory.Domain;
using DroneFactory.Storage;

namespace DroneFactory.Commands;

/// <summary>
/// Parses ARGS into (drone name, quantity, effective template) triples, supporting the
/// WITH/WITHOUT/REPLACE drone modifiers (readme.md §5.2.1). When none of these keywords are
/// present, falls back to the original comma-separated, duplicate-summing format (§3.1) via
/// <see cref="ArgsParser"/> unchanged, so every pre-phase-3 instruction keeps working exactly as
/// before. When modifiers are used, the separator switches to ';' and each entry is resolved
/// independently rather than merged by name, since two entries for the same drone could carry
/// different modifiers — see docs/HYPOTHESES.md.
/// </summary>
public static class DroneOrderParser
{
    private static readonly Regex ModifierKeyword = new(@"\b(WITHOUT|WITH|REPLACE)\b", RegexOptions.Compiled);

    public static bool TryParse(
        string args,
        ITemplateRepository templates,
        out List<(string Name, int Quantity, DroneTemplate Template)> order,
        out string? error)
    {
        order = new List<(string, int, DroneTemplate)>();
        error = null;

        if (string.IsNullOrWhiteSpace(args))
        {
            error = "Missing arguments";
            return false;
        }

        if (!ModifierKeyword.IsMatch(args) && !args.Contains(';'))
        {
            if (!ArgsParser.TryParse(args, out var quantities, out var parseError))
            {
                error = parseError;
                return false;
            }

            foreach (var (name, quantity) in quantities)
            {
                var template = templates.Find(name);
                if (template is null)
                {
                    error = $"`{name}` is not a recognized drone";
                    order = new List<(string, int, DroneTemplate)>();
                    return false;
                }

                order.Add((name, quantity, template));
            }

            return true;
        }

        foreach (var segment in args.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParseSegment(segment, templates, out var entry, out error))
            {
                order = new List<(string, int, DroneTemplate)>();
                return false;
            }

            order.Add(entry);
        }

        return true;
    }

    private static bool TryParseSegment(
        string segment,
        ITemplateRepository templates,
        out (string Name, int Quantity, DroneTemplate Template) entry,
        out string? error)
    {
        entry = default;
        error = null;

        var matches = ModifierKeyword.Matches(segment);
        var head = (matches.Count == 0 ? segment : segment[..matches[0].Index]).Trim();

        var headParts = head.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (headParts.Length != 2 || !int.TryParse(headParts[0], out var quantity) || quantity <= 0)
        {
            error = $"Invalid entry '{segment}', expected format '<quantity> <DroneName>'";
            return false;
        }

        var name = headParts[1];
        var template = templates.Find(name);
        if (template is null)
        {
            error = $"`{name}` is not a recognized drone";
            return false;
        }

        if (matches.Count == 0)
        {
            entry = (name, quantity, template);
            return true;
        }

        var bag = template.RequiredPieces.ToList();

        for (var i = 0; i < matches.Count; i++)
        {
            var keyword = matches[i].Groups[1].Value;
            var blockStart = matches[i].Index + matches[i].Length;
            var blockEnd = i + 1 < matches.Count ? matches[i + 1].Index : segment.Length;
            var block = segment[blockStart..blockEnd].Trim();

            if (!TryParsePieceList(block, out var pieces, out error))
            {
                return false;
            }

            if (!TryApplyModifier(keyword, pieces, bag, out error))
            {
                return false;
            }
        }

        bag.Add(template.System);

        if (!DroneTemplateBuilder.TryBuild(template.Name, bag, out var effective, out error))
        {
            return false;
        }

        entry = (name, quantity, effective!);
        return true;
    }

    private static bool TryApplyModifier(string keyword, List<(int Quantity, string Piece)> pieces, List<string> bag, out string? error)
    {
        error = null;

        switch (keyword)
        {
            case "WITH":
                foreach (var (pieceQty, pieceName) in pieces)
                {
                    for (var k = 0; k < pieceQty; k++)
                    {
                        bag.Add(pieceName);
                    }
                }

                return true;

            case "WITHOUT":
                foreach (var (pieceQty, pieceName) in pieces)
                {
                    if (!TryRemove(bag, pieceName, pieceQty, out error))
                    {
                        return false;
                    }
                }

                return true;

            case "REPLACE":
                if (pieces.Count % 2 != 0)
                {
                    error = "REPLACE requires pairs of '<quantity> <Piece>' entries (one removed, one added)";
                    return false;
                }

                for (var p = 0; p < pieces.Count; p += 2)
                {
                    var (removeQty, removePiece) = pieces[p];
                    var (addQty, addPiece) = pieces[p + 1];

                    if (!TryRemove(bag, removePiece, removeQty, out error))
                    {
                        return false;
                    }

                    for (var k = 0; k < addQty; k++)
                    {
                        bag.Add(addPiece);
                    }
                }

                return true;

            default:
                error = $"Unknown modifier `{keyword}`";
                return false;
        }
    }

    private static bool TryRemove(List<string> bag, string piece, int quantity, out string? error)
    {
        var available = bag.Count(p => p == piece);
        if (available < quantity)
        {
            error = $"Cannot remove {quantity} {piece}: only {available} present in this drone's composition";
            return false;
        }

        for (var i = 0; i < quantity; i++)
        {
            bag.Remove(piece);
        }

        error = null;
        return true;
    }

    private static bool TryParsePieceList(string block, out List<(int Quantity, string Piece)> pieces, out string? error)
    {
        pieces = new List<(int, string)>();
        error = null;

        if (string.IsNullOrWhiteSpace(block))
        {
            error = "Expected at least one '<quantity> <Piece>' entry after the modifier keyword";
            return false;
        }

        foreach (var rawEntry in block.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = rawEntry.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var quantity) || quantity <= 0)
            {
                error = $"Invalid entry '{rawEntry}', expected format '<quantity> <Piece>'";
                return false;
            }

            pieces.Add((quantity, parts[1]));
        }

        return true;
    }
}
