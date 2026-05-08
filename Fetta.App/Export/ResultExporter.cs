using System.Globalization;
using System.Text;
using Fetta.App.Core;
using Fetta.App.Localization;

namespace Fetta.App.Export;

public static class ResultExporter
{
    /// <summary>
    /// Exports the result to a file. Format is inferred from the file extension:
    ///   .csv → CSV  |  any other extension → plain text
    /// </summary>
    public static void Export(AllocationResult result, string filePath, Localizer localizer)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".csv")
            ExportCsv(result, filePath);
        else
            ExportTxt(result, filePath, localizer);
    }

    // ── CSV ──────────────────────────────────────────────────────────────────

    private static void ExportCsv(AllocationResult result, string filePath)
    {
        var sb = new StringBuilder();

        // Summary header
        sb.AppendLine($"# Totale peso,{F(result.TotalWeightKg)} kg");
        sb.AppendLine($"# Pacchi in ingresso,{result.TotalInputPackageCount}");
        sb.AppendLine($"# Pacchi distribuiti,{result.TotalAssignedPackageCount}");
        sb.AppendLine($"# Errore assoluto totale,{F(result.TotalAbsoluteErrorKg)} kg");
        sb.AppendLine($"# Strategia,{result.StrategyUsed}");
        sb.AppendLine();

        // Column headers
        sb.AppendLine(
            "Alias,Proporzione,Target (kg),Assegnato (kg),Delta (kg),N. Pacchi,Composizione"
        );

        foreach (var part in result.Parts)
        {
            var delta = part.AssignedWeightKg - part.TargetWeightKg;
            var breakdown = FormatBreakdown(part.BreakdownBySize);
            sb.AppendLine(
                $"{EscapeCsv(part.Alias)},{F(part.ProportionWeight)},{F(part.TargetWeightKg)},{F(part.AssignedWeightKg)},{FSign(delta)},{part.PackageCount},{EscapeCsv(breakdown)}"
            );
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    // ── TXT ──────────────────────────────────────────────────────────────────

    private static void ExportTxt(AllocationResult result, string filePath, Localizer localizer)
    {
        var sb = new StringBuilder();

        sb.AppendLine(localizer["result.header"]);
        sb.AppendLine(new string('-', 72));
        sb.AppendLine($"{localizer["result.totalWeight"]}: {F(result.TotalWeightKg)} kg");
        sb.AppendLine(
            $"{localizer["result.packageCheck"]}: {result.TotalInputPackageCount} {localizer["result.packagesIn"]} → {result.TotalAssignedPackageCount} {localizer["result.packagesOut"]}"
        );
        sb.AppendLine($"{localizer["result.totalError"]}: {F(result.TotalAbsoluteErrorKg)} kg");
        sb.AppendLine($"{localizer["result.strategy"]}: {result.StrategyUsed}");
        sb.AppendLine();

        foreach (var part in result.Parts)
        {
            var delta = part.AssignedWeightKg - part.TargetWeightKg;
            sb.AppendLine(
                $"[{part.Alias}]  {localizer["result.target"]}: {F(part.TargetWeightKg)} kg  |  {localizer["result.assigned"]}: {F(part.AssignedWeightKg)} kg  |  {localizer["result.delta"]}: {FSign(delta)} kg  |  {localizer["result.nPackages"]}: {part.PackageCount}"
            );
            sb.AppendLine(
                $"  {localizer["result.breakdown"]}: {FormatBreakdown(part.BreakdownBySize)}"
            );
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static string F(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FSign(decimal value) => (value > 0 ? "+" : string.Empty) + F(value);

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string FormatBreakdown(IReadOnlyDictionary<decimal, int> breakdown)
    {
        if (breakdown.Count == 0)
            return "-";
        return string.Join(
            " + ",
            breakdown
                .OrderByDescending(kv => kv.Key)
                .Select(kv =>
                    $"{kv.Value}x{kv.Key.ToString("0.###", CultureInfo.InvariantCulture)}kg"
                )
        );
    }
}
