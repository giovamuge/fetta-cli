using Fetta.App.Core;

namespace Fetta.Tests.Core;

public class AllocationSolverTests
{
    [Fact]
    public void Solve_AllPackagesAlwaysAssigned()
    {
        var solver = new AllocationSolver();
        var packages = new List<PackageType> { new(5m, 2), new(6m, 1) };
        var proportions = new List<NamedProportion> { new("A", 1m), new("B", 1m) };

        var result = solver.Solve(packages, proportions);

        Assert.Equal(3, result.TotalInputPackageCount);
        Assert.Equal(3, result.TotalAssignedPackageCount);
        Assert.Equal(16m, result.TotalWeightKg);
    }

    [Fact]
    public void Solve_PackageCountEqualsInput_WhenOddDistribution()
    {
        var solver = new AllocationSolver();
        var packages = new List<PackageType> { new(5m, 1) };
        var proportions = new List<NamedProportion> { new("X", 1m), new("Y", 1m), new("Z", 1m) };

        var result = solver.Solve(packages, proportions);

        // The single package must be assigned to exactly one part
        Assert.Equal(1, result.TotalInputPackageCount);
        Assert.Equal(1, result.TotalAssignedPackageCount);
        Assert.Equal(5m, result.TotalWeightKg);
        Assert.Single(result.Parts, p => p.PackageCount == 1);
    }

    [Fact]
    public void Solve_AliasesPropagate_ToPartAllocation()
    {
        var solver = new AllocationSolver();
        var packages = new List<PackageType> { new(6m, 2) };
        var proportions = new List<NamedProportion> { new("Mario", 1m), new("Luigi", 1m) };

        var result = solver.Solve(packages, proportions);

        Assert.Equal("Mario", result.Parts[0].Alias);
        Assert.Equal("Luigi", result.Parts[1].Alias);
    }

    [Fact]
    public void Solve_EqualProportions_DistributesEvenly()
    {
        var solver = new AllocationSolver();
        var packages = new List<PackageType> { new(5m, 2) };
        var proportions = new List<NamedProportion> { new("A", 1m), new("B", 1m) };

        var result = solver.Solve(packages, proportions);

        Assert.Equal(0m, result.TotalAbsoluteErrorKg);
        Assert.All(result.Parts, p => Assert.Equal(5m, p.AssignedWeightKg));
    }

    [Fact]
    public void Solve_StrategyIsExact_ForSmallInput()
    {
        var solver = new AllocationSolver();
        var packages = new List<PackageType> { new(3m, 3) };
        var proportions = new List<NamedProportion> { new("P1", 1m), new("P2", 2m) };

        var result = solver.Solve(packages, proportions);

        Assert.Equal("exact", result.StrategyUsed);
    }
}
