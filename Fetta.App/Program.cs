using System.Globalization;
using Fetta.App.Core;
using Fetta.App.Export;
using Fetta.App.Localization;
using Fetta.App.Parsing;
using Fetta.App.Storage;

var options = ParseArgs(args);
var lang = options.TryGetValue("lang", out var parsedLang) ? parsedLang : "it";
var iniPath = options.TryGetValue("ini", out var iniArg) ? iniArg : ProportionStore.DefaultPath;
var outputPath = options.TryGetValue("output", out var outArg) ? outArg : null;
var localizer = Localizer.ForLanguage(lang);

try
{
    var interactive = args.Length == 0 || options.ContainsKey("interactive");

    string rawPackages;
    IReadOnlyList<NamedProportion> proportions;

    if (interactive)
    {
        Console.WriteLine(localizer["app.title"]);
        Console.WriteLine();

        Console.WriteLine(localizer["prompt.packages"]);
        rawPackages = Console.ReadLine() ?? string.Empty;

        proportions = ResolveProportionsInteractive(iniPath, localizer);
    }
    else
    {
        if (
            !options.TryGetValue("packages", out var rawPackagesValue)
            || string.IsNullOrWhiteSpace(rawPackagesValue)
        )
            throw new ArgumentException("Missing required argument --packages.");

        rawPackages = rawPackagesValue;

        if (options.TryGetValue("proportions", out var rawProportionsValue))
        {
            proportions = ProportionParser.ParseNamed(rawProportionsValue);
            ProportionStore.Save(proportions, iniPath);
        }
        else
        {
            var saved = ProportionStore.Load(iniPath);
            if (saved is null || saved.Count == 0)
                throw new ArgumentException(
                    "No proportions provided and no INI file found. Pass --proportions."
                );
            proportions = saved;
        }
    }

    var packageTypes = PackageCatalogParser.Parse(rawPackages);
    var solver = new AllocationSolver();
    var result = solver.Solve(packageTypes, proportions);

    PrintResult(result, localizer);

    if (outputPath is not null)
    {
        ResultExporter.Export(result, outputPath, localizer);
        Console.WriteLine($"{localizer["result.saved"]}: {outputPath}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"{localizer["error.prefix"]}: {ex.Message}");
    Environment.ExitCode = 1;
}

// ── Interactive proportion resolution ─────────────────────────────────────────

static IReadOnlyList<NamedProportion> ResolveProportionsInteractive(
    string iniPath,
    Localizer localizer
)
{
    var saved = ProportionStore.Load(iniPath);

    if (saved is { Count: > 0 })
    {
        Console.WriteLine();
        Console.WriteLine(localizer["prompt.proportions.existing"]);
        foreach (var p in saved)
            Console.WriteLine($"  {p.Alias} = {p.Weight}");

        Console.WriteLine();
        Console.Write(localizer["prompt.proportions.modify"] + " ");
        var answer = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();

        if (answer != "s" && answer != "si" && answer != "y" && answer != "yes")
            return saved;
    }

    Console.WriteLine(localizer["prompt.proportions"]);
    Console.WriteLine(localizer["prompt.proportions.alias"]);
    var rawInput = Console.ReadLine() ?? string.Empty;
    var proportions = ProportionParser.ParseNamed(rawInput);

    ProportionStore.Save(proportions, iniPath);
    Console.WriteLine($"{localizer["prompt.proportions.saved"]}: {iniPath}");
    Console.WriteLine();

    return proportions;
}

// ── CLI argument parser ────────────────────────────────────────────────────────

static Dictionary<string, string> ParseArgs(string[] args)
{
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var token = args[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
            continue;

        var key = token[2..];
        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            map[key] = args[i + 1];
            i++;
        }
        else
        {
            map[key] = "true";
        }
    }

    return map;
}

// ── Console output ────────────────────────────────────────────────────────────

static void PrintResult(AllocationResult result, Localizer localizer)
{
    var totalProportion = result.Parts.Sum(p => p.ProportionWeight);

    Console.WriteLine();
    WriteColor(localizer["result.header"], ConsoleColor.Cyan);
    Console.WriteLine(new string('─', 72));

    // kg summary
    var totalAssignedKg = result.Parts.Sum(p => p.AssignedWeightKg);
    Console.Write($"Peso in ingresso: ");
    WriteColor(FormatKg(result.TotalWeightKg), ConsoleColor.White);
    Console.Write($"  →  Peso distribuito: ");
    WriteColor(FormatKg(totalAssignedKg), ConsoleColor.White);
    Console.WriteLine();

    // package count check
    var countOk = result.TotalAssignedPackageCount == result.TotalInputPackageCount;
    var countMark = countOk ? "✓" : "✗";
    var countColor = countOk ? ConsoleColor.Green : ConsoleColor.Red;
    Console.Write($"{localizer["result.packageCheck"]}: ");
    WriteColor(
        $"{countMark}  {result.TotalInputPackageCount} pacchi → {result.TotalAssignedPackageCount} pacchi",
        countColor
    );
    Console.WriteLine();

    Console.WriteLine(
        $"{localizer["result.totalError"]}: {FormatKg(result.TotalAbsoluteErrorKg)}  |  {localizer["result.strategy"]}: {result.StrategyUsed}"
    );
    Console.WriteLine(new string('─', 72));
    Console.WriteLine();

    foreach (var part in result.Parts)
    {
        var delta = part.AssignedWeightKg - part.TargetWeightKg;
        var pct = totalProportion > 0 ? (part.ProportionWeight / totalProportion * 100m) : 0m;
        var pctStr = pct.ToString("0.#", CultureInfo.InvariantCulture) + "%";

        // Line 1: alias (colored) + composition on the same line
        Console.Write("  ");
        WriteColor($"[{part.Alias}]", ConsoleColor.Yellow);
        Console.Write($" ({pctStr})");
        Console.Write("  ");
        WriteColor(FormatBreakdown(part.BreakdownBySize), ConsoleColor.Magenta);
        Console.WriteLine();

        // Line 2: metrics
        Console.Write("    ");
        Console.Write($"{localizer["result.target"]}: {FormatKg(part.TargetWeightKg)}  |  ");
        Console.Write($"{localizer["result.assigned"]}: ");
        WriteColor(FormatKg(part.AssignedWeightKg), ConsoleColor.Green);
        Console.Write($"  |  {localizer["result.delta"]}: ");
        var deltaColor =
            delta > 0 ? ConsoleColor.Red
            : delta < 0 ? ConsoleColor.DarkYellow
            : ConsoleColor.Green;
        WriteColor(FormatSignedKg(delta), deltaColor);
        Console.Write($"  |  {localizer["result.nPackages"]}: {part.PackageCount}");
        Console.WriteLine();
        Console.WriteLine();
    }
}

static void WriteColor(string text, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ResetColor();
}

static string FormatKg(decimal value) =>
    value.ToString("0.###", CultureInfo.InvariantCulture) + " kg";

static string FormatSignedKg(decimal value) => (value > 0 ? "+" : string.Empty) + FormatKg(value);

static string FormatBreakdown(IReadOnlyDictionary<decimal, int> breakdown)
{
    if (breakdown.Count == 0)
        return "-";

    return string.Join(
        " + ",
        breakdown
            .OrderByDescending(kv => kv.Key)
            .Select(kv => $"{kv.Value}x{kv.Key.ToString("0.###", CultureInfo.InvariantCulture)}kg")
    );
}
