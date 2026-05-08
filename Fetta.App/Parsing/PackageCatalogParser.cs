using System.Globalization;
using Fetta.App.Core;

namespace Fetta.App.Parsing;

public static class PackageCatalogParser
{
    public static IReadOnlyList<PackageType> Parse(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            throw new ArgumentException("Package catalog cannot be empty.");
        }

        var chunks = rawInput.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        var result = new List<PackageType>(chunks.Length);

        foreach (var chunk in chunks)
        {
            var pair = chunk.Split(
                ':',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (pair.Length != 2)
            {
                throw new ArgumentException(
                    $"Invalid package entry '{chunk}'. Expected format weight:count."
                );
            }

            if (
                !decimal.TryParse(
                    pair[0],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var weightKg
                )
                || weightKg <= 0
            )
            {
                throw new ArgumentException($"Invalid package weight '{pair[0]}'.");
            }

            if (
                !int.TryParse(
                    pair[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var count
                )
                || count < 0
            )
            {
                throw new ArgumentException($"Invalid package count '{pair[1]}'.");
            }

            if (count == 0)
            {
                continue;
            }

            result.Add(new PackageType(weightKg, count));
        }

        if (result.Count == 0)
        {
            throw new ArgumentException("Package catalog produced no available packages.");
        }

        return result;
    }
}
