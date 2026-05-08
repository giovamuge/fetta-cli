using Fetta.App.Parsing;

namespace Fetta.Tests.Parsing;

public class ProportionParserTests
{
    [Fact]
    public void ParseNamed_RatioFormat_Works()
    {
        var values = ProportionParser.ParseNamed("2:3:5");

        Assert.Equal(new decimal[] { 2m, 3m, 5m }, values.Select(p => p.Weight));
        Assert.Equal(new[] { "Parte 1", "Parte 2", "Parte 3" }, values.Select(p => p.Alias));
    }

    [Fact]
    public void ParseNamed_CsvFormat_Works()
    {
        var values = ProportionParser.ParseNamed("1,1,2");

        Assert.Equal(new decimal[] { 1m, 1m, 2m }, values.Select(p => p.Weight));
    }

    [Fact]
    public void ParseNamed_PercentStyle_Works()
    {
        var values = ProportionParser.ParseNamed("20%,30%,50%");

        Assert.Equal(new decimal[] { 20m, 30m, 50m }, values.Select(p => p.Weight));
    }

    [Fact]
    public void ParseNamed_WithAliasesCsv_Works()
    {
        var values = ProportionParser.ParseNamed("Alice=2,Bob=3,Carlo=5");

        Assert.Equal(3, values.Count);
        Assert.Equal("Alice", values[0].Alias);
        Assert.Equal(2m, values[0].Weight);
        Assert.Equal("Bob", values[1].Alias);
        Assert.Equal("Carlo", values[2].Alias);
        Assert.Equal(5m, values[2].Weight);
    }
}
