using System.Globalization;

namespace Fetta.App.Core;

public sealed record PackageType(decimal WeightKg, int AvailableCount)
{
    public override string ToString() =>
        $"{WeightKg.ToString("0.###", CultureInfo.InvariantCulture)}kg x {AvailableCount}";
}

public sealed record PartAllocation(
    int PartIndex,
    string Alias,
    decimal ProportionWeight,
    decimal TargetWeightKg,
    decimal AssignedWeightKg,
    IReadOnlyDictionary<decimal, int> BreakdownBySize
)
{
    /// <summary>Total number of packages assigned to this part.</summary>
    public int PackageCount => BreakdownBySize.Values.Sum();
}

/// <summary>
/// Result of an allocation run.  All input packages are always assigned —
/// there is no leftover.  <see cref="TotalAssignedPackageCount"/> must equal
/// <see cref="TotalInputPackageCount"/> by construction.
/// </summary>
public sealed record AllocationResult(
    IReadOnlyList<PartAllocation> Parts,
    decimal TotalWeightKg,
    int TotalInputPackageCount,
    decimal TotalAbsoluteErrorKg,
    string StrategyUsed
)
{
    public int TotalAssignedPackageCount => Parts.Sum(p => p.PackageCount);
}
