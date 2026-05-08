using System.Globalization;
using Fetta.App.Core;

namespace Fetta.App.Parsing;

public static class ProportionParser
{
    /// <summary>
    /// Parses named proportions.
    ///
    /// Supported formats:
    ///   With aliases : "Alice=2,Bob=3,Carlo=5"  or  "Alice=2:Bob=3:Carlo=5"
    ///   Without      : "2:3:5"  or  "2,3,5"  or  "20%,30%,50%"
    ///   (auto-names  : "Parte 1", "Parte 2", …)
    /// </summary>
    public static IReadOnlyList<NamedProportion> ParseNamed(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            throw new ArgumentException("Proportion input cannot be empty.");
        }

        var normalized = rawInput.Trim();
        var separator = normalized.Contains(':') && !normalized.Contains('=') ? ':' : ',';

        // If the input uses ':' but also has '=' (alias=value) switch to ':' separator only
        // when none of the tokens contain '='.
        if (normalized.Contains(':') && normalized.Contains('='))
        {
            separator = ':';
        }

        var tokens = normalized.Split(
            separator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        if (tokens.Length == 0)
        {
            throw new ArgumentException("No proportions were found.");
        }

        var values = new List<NamedProportion>(tokens.Length);

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            string alias;
            string rawValue;

            var eqIndex = token.IndexOf('=');
            if (eqIndex > 0)
            {
                alias = token[..eqIndex].Trim();
                rawValue = token[(eqIndex + 1)..].Trim();
            }
            else
            {
                alias = $"Parte {i + 1}";
                rawValue = token.Trim();
            }

            var cleaned = rawValue.TrimEnd('%');
            if (
                !decimal.TryParse(
                    cleaned,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var value
                )
            )
            {
                throw new ArgumentException($"Invalid proportion value: '{token}'.");
            }

            if (value <= 0)
            {
                throw new ArgumentException("All proportions must be greater than zero.");
            }

            values.Add(new NamedProportion(alias, value));
        }

        return values;
    }
}
