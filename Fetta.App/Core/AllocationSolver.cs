namespace Fetta.App.Core;

public sealed class AllocationSolver
{
    // Keep DFS nodes under ~500 000: maxItems = floor(log(500000) / log(partCount))
    private static int ExactMaxItems(int partCount) =>
        partCount <= 1 ? 64 : (int)(Math.Log(500_000.0) / Math.Log(partCount));

    public AllocationResult Solve(
        IReadOnlyList<PackageType> packageTypes,
        IReadOnlyList<NamedProportion> proportions
    )
    {
        if (packageTypes.Count == 0)
            throw new ArgumentException("At least one package type is required.");

        if (proportions.Count == 0)
            throw new ArgumentException("At least one proportion is required.");

        var normalizedPackages = packageTypes.OrderByDescending(p => p.WeightKg).ToList();
        var totalPackageCount = normalizedPackages.Sum(p => p.AvailableCount);
        var totalWeight = normalizedPackages.Sum(p => p.WeightKg * p.AvailableCount);

        var totalProportion = proportions.Sum(p => p.Weight);
        var targets = proportions.Select(p => totalWeight * (p.Weight / totalProportion)).ToArray();

        var items = ExpandItems(normalizedPackages);
        var partCount = proportions.Count;
        var threshold = ExactMaxItems(partCount);

        int[] partAssignment;
        string strategy;

        if (items.Count <= threshold)
        {
            partAssignment = SolveExact(items, targets);
            strategy = "exact";
        }
        else
        {
            partAssignment = SolveGreedyWithSwaps(items, targets);
            strategy = "greedy+swaps";
        }

        if (partAssignment.Length != items.Count)
            throw new InvalidOperationException("Internal error: item count mismatch.");

        return BuildResult(
            partAssignment,
            items,
            normalizedPackages,
            proportions,
            targets,
            totalWeight,
            totalPackageCount,
            strategy
        );
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static List<decimal> ExpandItems(IReadOnlyList<PackageType> packageTypes)
    {
        var items = new List<decimal>();
        foreach (var pkg in packageTypes)
            for (var i = 0; i < pkg.AvailableCount; i++)
                items.Add(pkg.WeightKg);
        items.Sort((a, b) => b.CompareTo(a));
        return items;
    }

    private static decimal ComputeAbsoluteError(decimal[] sums, decimal[] targets)
    {
        var total = 0m;
        for (var i = 0; i < sums.Length; i++)
            total += Math.Abs(sums[i] - targets[i]);
        return decimal.Round(total, 6, MidpointRounding.AwayFromZero);
    }

    // ── Exact DFS ───────────────────────────────────────────────────────────────

    private static int[] SolveExact(IReadOnlyList<decimal> items, decimal[] targets)
    {
        var n = items.Count;
        var p = targets.Length;
        var partSums = new decimal[p];
        var currentAssignment = new int[n];
        var bestAssignment = new int[n];
        var bestError = decimal.MaxValue;

        void Dfs(int index)
        {
            if (index == n)
            {
                var err = ComputeAbsoluteError(partSums, targets);
                if (err < bestError)
                {
                    bestError = err;
                    Array.Copy(currentAssignment, bestAssignment, n);
                }
                return;
            }

            var weight = items[index];
            decimal? lastSum = null;
            decimal? lastTarget = null;

            for (var part = 0; part < p; part++)
            {
                var s = partSums[part];
                var t = targets[part];
                // Two parts are symmetric only if both current sum and target are equal.
                if (lastSum.HasValue && s == lastSum.Value && t == lastTarget!.Value)
                    continue;
                lastSum = s;
                lastTarget = t;
                partSums[part] += weight;
                currentAssignment[index] = part;
                Dfs(index + 1);
                partSums[part] -= weight;
            }
        }

        Dfs(0);
        return bestAssignment;
    }

    // ── Greedy + iterative local improvement ────────────────────────────────────

    private static int[] SolveGreedyWithSwaps(IReadOnlyList<decimal> items, decimal[] targets)
    {
        var n = items.Count;
        var p = targets.Length;
        var assignment = new int[n];
        var partSums = new decimal[p];

        for (var i = 0; i < n; i++)
        {
            var bestPart = 0;
            var bestGap = decimal.MinValue;
            for (var part = 0; part < p; part++)
            {
                var gap = targets[part] - partSums[part];
                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestPart = part;
                }
            }
            assignment[i] = bestPart;
            partSums[bestPart] += items[i];
        }

        var improved = true;
        while (improved)
        {
            improved = false;
            var currentError = ComputeAbsoluteError(partSums, targets);

            for (var i = 0; i < n && !improved; i++)
            {
                for (var j = i + 1; j < n && !improved; j++)
                {
                    var pi = assignment[i];
                    var pj = assignment[j];
                    if (pi == pj)
                        continue;

                    partSums[pi] += items[j] - items[i];
                    partSums[pj] += items[i] - items[j];

                    if (ComputeAbsoluteError(partSums, targets) < currentError)
                    {
                        (assignment[i], assignment[j]) = (assignment[j], assignment[i]);
                        improved = true;
                    }
                    else
                    {
                        partSums[pi] += items[i] - items[j];
                        partSums[pj] += items[j] - items[i];
                    }
                }
            }

            for (var i = 0; i < n && !improved; i++)
            {
                var pi = assignment[i];
                for (var part = 0; part < p && !improved; part++)
                {
                    if (part == pi)
                        continue;

                    partSums[pi] -= items[i];
                    partSums[part] += items[i];

                    if (ComputeAbsoluteError(partSums, targets) < currentError)
                    {
                        assignment[i] = part;
                        improved = true;
                    }
                    else
                    {
                        partSums[pi] += items[i];
                        partSums[part] -= items[i];
                    }
                }
            }
        }

        return assignment;
    }

    // ── Result construction ──────────────────────────────────────────────────────

    private static AllocationResult BuildResult(
        int[] partAssignment,
        IReadOnlyList<decimal> items,
        IReadOnlyList<PackageType> packageTypes,
        IReadOnlyList<NamedProportion> proportions,
        decimal[] targets,
        decimal totalWeight,
        int totalPackageCount,
        string strategy
    )
    {
        var p = proportions.Count;
        var partSums = new decimal[p];
        var breakdowns = Enumerable
            .Range(0, p)
            .Select(_ => new Dictionary<decimal, int>())
            .ToArray();

        for (var i = 0; i < items.Count; i++)
        {
            var part = partAssignment[i];
            partSums[part] += items[i];
            breakdowns[part].TryGetValue(items[i], out var cnt);
            breakdowns[part][items[i]] = cnt + 1;
        }

        var parts = new List<PartAllocation>(p);
        for (var part = 0; part < p; part++)
        {
            parts.Add(
                new PartAllocation(
                    part + 1,
                    proportions[part].Alias,
                    proportions[part].Weight,
                    targets[part],
                    partSums[part],
                    breakdowns[part]
                )
            );
        }

        return new AllocationResult(
            parts,
            totalWeight,
            totalPackageCount,
            ComputeAbsoluteError(partSums, targets),
            strategy
        );
    }
}
