using Fetta.App.Core;

namespace Fetta.App.Storage;

/// <summary>
/// Reads and writes named proportions to a simple INI file.
///
/// File format:
/// [proportions]
/// AliasA=2
/// AliasB=3
/// AliasC=5
/// </summary>
public static class ProportionStore
{
    private const string Section = "[proportions]";

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "fetta.ini");

    public static IReadOnlyList<NamedProportion>? Load(string? path = null)
    {
        var filePath = path ?? DefaultPath;
        if (!File.Exists(filePath))
        {
            return null;
        }

        var lines = File.ReadAllLines(filePath);
        var inSection = false;
        var result = new List<NamedProportion>();

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.StartsWith('['))
            {
                inSection = line.Equals(Section, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inSection || line.StartsWith(';') || line.StartsWith('#') || line.Length == 0)
            {
                continue;
            }

            var eqIndex = line.IndexOf('=');
            if (eqIndex <= 0)
            {
                continue;
            }

            var alias = line[..eqIndex].Trim();
            var rawValue = line[(eqIndex + 1)..].Trim();

            if (
                decimal.TryParse(
                    rawValue,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var weight
                )
                && weight > 0
            )
            {
                result.Add(new NamedProportion(alias, weight));
            }
        }

        return result.Count > 0 ? result : null;
    }

    public static void Save(IReadOnlyList<NamedProportion> proportions, string? path = null)
    {
        var filePath = path ?? DefaultPath;

        var lines = new List<string> { Section };
        foreach (var p in proportions)
        {
            lines.Add(
                $"{p.Alias}={p.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            );
        }

        File.WriteAllLines(filePath, lines);
    }
}
