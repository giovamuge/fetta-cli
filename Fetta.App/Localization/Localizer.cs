namespace Fetta.App.Localization;

public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, string> _messages;

    private Localizer(IReadOnlyDictionary<string, string> messages)
    {
        _messages = messages;
    }

    public static Localizer ForLanguage(string? language)
    {
        var lang = (language ?? "it").Trim().ToLowerInvariant();
        return lang switch
        {
            "en" => new Localizer(English),
            _ => new Localizer(Italian),
        };
    }

    public string this[string key] => _messages.TryGetValue(key, out var value) ? value : key;

    private static readonly IReadOnlyDictionary<string, string> Italian = new Dictionary<
        string,
        string
    >
    {
        ["app.title"] = "Ripartizione pacchi indivisibili",
        ["prompt.packages"] =
            "Inserisci pacchi (formato peso:quantita, separati da virgola, es. 5:2,6:3):",
        ["prompt.proportions"] =
            "Inserisci proporzioni con alias (es. Alice=2,Bob=3,Carlo=5  oppure  2:3:5):",
        ["prompt.proportions.alias"] =
            "  Formato suggerito: NomeAlias=valore separati da virgola o due punti",
        ["prompt.proportions.existing"] = "Proporzioni salvate:",
        ["prompt.proportions.modify"] = "Vuoi modificarle? (s/n):",
        ["prompt.proportions.saved"] = "Proporzioni salvate in",
        ["result.header"] = "Risultato allocazione",
        ["result.totalWeight"] = "Peso totale",
        ["result.packageCheck"] = "Controllo pacchi",
        ["result.packagesIn"] = "in ingresso",
        ["result.packagesOut"] = "distribuiti",
        ["result.totalError"] = "Errore assoluto totale (kg)",
        ["result.strategy"] = "Strategia",
        ["result.part"] = "Parte",
        ["result.target"] = "Target",
        ["result.assigned"] = "Assegnato",
        ["result.delta"] = "Delta",
        ["result.nPackages"] = "Pacchi",
        ["result.breakdown"] = "Composizione",
        ["result.saved"] = "Risultato salvato in",
        ["error.prefix"] = "Errore",
    };

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<
        string,
        string
    >
    {
        ["app.title"] = "Indivisible package allocation",
        ["prompt.packages"] =
            "Enter packages (format weight:count, comma-separated, e.g. 5:2,6:3):",
        ["prompt.proportions"] =
            "Enter proportions with aliases (e.g. Alice=2,Bob=3,Carlo=5  or  2:3:5):",
        ["prompt.proportions.alias"] =
            "  Suggested format: AliasName=value separated by commas or colons",
        ["prompt.proportions.existing"] = "Saved proportions:",
        ["prompt.proportions.modify"] = "Modify them? (y/n):",
        ["prompt.proportions.saved"] = "Proportions saved to",
        ["result.header"] = "Allocation result",
        ["result.totalWeight"] = "Total weight",
        ["result.packageCheck"] = "Package check",
        ["result.packagesIn"] = "in",
        ["result.packagesOut"] = "distributed",
        ["result.totalError"] = "Total absolute error (kg)",
        ["result.strategy"] = "Strategy",
        ["result.part"] = "Part",
        ["result.target"] = "Target",
        ["result.assigned"] = "Assigned",
        ["result.delta"] = "Delta",
        ["result.nPackages"] = "Packages",
        ["result.breakdown"] = "Breakdown",
        ["result.saved"] = "Result saved to",
        ["error.prefix"] = "Error",
    };
}
