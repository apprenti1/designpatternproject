using DroneFactory.Commands;

namespace DroneFactory.Tests;

public class ArgsParserTests
{
    [Fact]
    public void ParsesASingleEntry()
    {
        var ok = ArgsParser.TryParse("1 DXF-1", out var quantities, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(new Dictionary<string, int> { ["DXF-1"] = 1 }, quantities);
    }

    [Fact]
    public void ParsesMultipleEntries()
    {
        var ok = ArgsParser.TryParse("1 DXF-1, 2 RDL-1", out var quantities, out _);

        Assert.True(ok);
        Assert.Equal(new Dictionary<string, int> { ["DXF-1"] = 1, ["RDL-1"] = 2 }, quantities);
    }

    [Fact]
    public void SumsDuplicateDronesInTheSameOrder()
    {
        // readme.md §3.1: "A Drone1, B Drone2, C Drone1" must be treated as "A+C Drone1, B Drone2".
        var ok = ArgsParser.TryParse("1 DXF-1, 2 RDL-1, 3 DXF-1", out var quantities, out _);

        Assert.True(ok);
        Assert.Equal(new Dictionary<string, int> { ["DXF-1"] = 4, ["RDL-1"] = 2 }, quantities);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("DXF-1")]
    [InlineData("0 DXF-1")]
    [InlineData("-1 DXF-1")]
    [InlineData("one DXF-1")]
    public void RejectsMalformedInput(string args)
    {
        var ok = ArgsParser.TryParse(args, out _, out var error);

        Assert.False(ok);
        Assert.NotNull(error);
    }
}
