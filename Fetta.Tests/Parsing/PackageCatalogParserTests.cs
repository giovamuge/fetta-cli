using Fetta.App.Parsing;

namespace Fetta.Tests.Parsing;

public class PackageCatalogParserTests
{
    [Fact]
    public void Parse_ValidCatalog_Works()
    {
        var parsed = PackageCatalogParser.Parse("5:2,6:3");

        Assert.Equal(2, parsed.Count);
        Assert.Equal(5m, parsed[0].WeightKg);
        Assert.Equal(2, parsed[0].AvailableCount);
        Assert.Equal(6m, parsed[1].WeightKg);
        Assert.Equal(3, parsed[1].AvailableCount);
    }

    [Fact]
    public void Parse_InvalidEntry_Throws()
    {
        Assert.Throws<ArgumentException>(() => PackageCatalogParser.Parse("5-2"));
    }
}
