# Fetta

Console app per distribuire pacchi indivisibili a taglie fisse in più parti proporzionali, minimizzando lo scarto dalla proporzione target.

---

## Requisiti

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

---

## Installazione

```bash
git clone <repo-url>
cd fetta
dotnet build Fetta.slnx
```

---

## Utilizzo

### Modalità interattiva (default)

Avvia l'app senza argomenti: ti verrà chiesto di inserire pacchi e proporzioni.

```bash
dotnet run --project Fetta.App
```

Se esiste già un file `fetta.ini` con proporzioni salvate, l'app le mostra e chiede se modificarle.

---

### Modalità argomenti

Tutti i valori passati da riga di comando.

```bash
dotnet run --project Fetta.App -- [opzioni]
```

| Flag | Obbligatorio | Descrizione |
|------|------|-------------|
| `--packages` | sì | Catalogo pacchi: `peso:quantità` separati da virgola |
| `--proportions` | no* | Proporzioni con alias (vedi formati sotto). Se omesso si usa l'INI salvato |
| `--lang` | no | Lingua output: `it` (default) oppure `en` |
| `--ini` | no | Percorso file INI custom (default: `fetta.ini` nella cartella dell'eseguibile) |
| `--output` | no | Salva il risultato su file. Estensione `.csv` → CSV, qualsiasi altra → testo |

> \* Almeno uno tra `--proportions` e un file INI esistente è obbligatorio in modalità argomenti.

---

## Formato input pacchi

```
peso1:quantità1,peso2:quantità2,...
```

**Esempi:**

```
5:2,6:3        →  2 pacchi da 5 kg e 3 pacchi da 6 kg
10:5           →  5 pacchi da 10 kg
2.5:4,5:2,10:1 →  taglie multiple con pesi decimali
```

---

## Formato proporzioni

Le proporzioni accettano tre formati equivalenti, con o senza alias.

### Con alias (consigliato)

```
Alice=2,Bob=3,Carlo=5
Alice=2:Bob=3:Carlo=5
```

### Senza alias (auto-nomi: Parte 1, Parte 2, …)

```
2:3:5          →  rapporto
2,3,5          →  lista CSV
20%,30%,50%    →  percentuali
```

> Le proporzioni vengono normalizzate automaticamente: `2,3,5` è equivalente a `20%,30%,50%`.

---

## File INI

Le proporzioni vengono salvate automaticamente in `fetta.ini` ogni volta che vengono inserite o modificate.

```ini
[proportions]
Alice=2
Bob=3
Carlo=5
```

Il percorso di default è la cartella dell'eseguibile. Usa `--ini /percorso/custom.ini` per cambiarlo.

---

## Esempi pratici

### 1 — Distribuzione con alias, salvataggio CSV

```bash
dotnet run --project Fetta.App -- \
  --packages "5:2,6:3" \
  --proportions "Alice=2,Bob=3,Carlo=5" \
  --output risultato.csv
```

Output console:

```
Risultato allocazione
------------------------------------------------------------------------
Peso totale: 28 kg
Controllo pacchi: ✓  5 in ingresso → 5 distribuiti
Errore assoluto totale (kg): 4 kg
Strategia: exact

[Alice]  Target: 5.6 kg  |  Assegnato: 6 kg  |  Delta: +0.4 kg  |  Pacchi: 1
  Composizione: 1x6kg
[Bob]  Target: 8.4 kg  |  Assegnato: 10 kg  |  Delta: +1.6 kg  |  Pacchi: 2
  Composizione: 2x5kg
[Carlo]  Target: 14 kg  |  Assegnato: 12 kg  |  Delta: -2 kg  |  Pacchi: 2
  Composizione: 2x6kg
```

### 2 — Riuso proporzioni dall'INI, output TXT

```bash
dotnet run --project Fetta.App -- \
  --packages "10:6" \
  --output report.txt
```

### 3 — Lingua inglese

```bash
dotnet run --project Fetta.App -- \
  --packages "5:4,10:2" \
  --proportions "A=1,B=1" \
  --lang en
```

### 4 — INI custom

```bash
dotnet run --project Fetta.App -- \
  --packages "5:3" \
  --ini ~/miei-progetti/proporzioni.ini
```

---

## Algoritmo

| Caso | Strategia |
|------|-----------|
| Numero item ≤ soglia adattiva | **Exact**: DFS esaustivo con symmetry-breaking |
| Numero item > soglia | **Greedy + swaps**: assegnazione greedy seguita da miglioramento iterativo (swap 2-opt + spostamento singolo) |

La soglia adattiva dipende dal numero di parti: `floor(log(500000) / log(N_parti))` — assicura che il DFS rimanga sotto ~500 000 nodi.

**Obiettivo**: minimizzare l'errore assoluto totale rispetto ai target proporzionali.

**Vincolo**: tutti i pacchi in ingresso devono essere distribuiti (nessun resto).

---

## Formato output CSV

```
# Totale peso,28 kg
# Pacchi in ingresso,5
# Pacchi distribuiti,5
# Errore assoluto totale,4 kg
# Strategia,exact

Alias,Proporzione,Target (kg),Assegnato (kg),Delta (kg),N. Pacchi,Composizione
Alice,2,5.6,6,+0.4,1,1x6kg
Bob,3,8.4,10,+1.6,2,2x5kg
Carlo,5,14,12,-2,2,2x6kg
```

---

## Test

```bash
dotnet test Fetta.slnx
```

---

## Struttura progetto

```
Fetta.App/
  Program.cs                  # Entrypoint CLI
  Core/
    AllocationSolver.cs       # Motore di ottimizzazione
    Models.cs                 # Modelli dominio
    NamedProportion.cs        # Record alias + peso
  Parsing/
    ProportionParser.cs       # Parser proporzioni (3 formati + alias)
    PackageCatalogParser.cs   # Parser catalogo pacchi
  Storage/
    ProportionStore.cs        # Lettura/scrittura fetta.ini
  Export/
    ResultExporter.cs         # Export CSV / TXT
  Localization/
    Localizer.cs              # IT / EN

Fetta.Tests/
  Core/
    AllocationSolverTests.cs
  Parsing/
    ProportionParserTests.cs
    PackageCatalogParserTests.cs
```
