using DroneFactory.Assembly;
using DroneFactory.Domain;

namespace DroneFactory.Tests;

public class AssemblyPlannerTests
{
    private static readonly DroneTemplate Dxf1 = DroneCatalog.Find("DXF-1")!;

    [Fact]
    public void StartsWithProducingAndEndsWithFinished()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1).ToList();

        Assert.Equal("PRODUCING DXF-1", lines.First());
        Assert.Equal("FINISHED DXF-1", lines.Last());
    }

    [Fact]
    public void PullsEveryPieceFromStockBeforeItIsUsed()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1).ToList();

        foreach (var piece in Dxf1.RequiredPieces)
        {
            var getOutIndex = lines.IndexOf($"GET_OUT_STOCK 1 {piece}");
            var firstUseIndex = lines.FindIndex(l => !l.StartsWith("GET_OUT_STOCK") && l.Contains(piece));

            Assert.True(getOutIndex >= 0, $"missing GET_OUT_STOCK for {piece}");
            Assert.True(getOutIndex < firstUseIndex, $"{piece} used before being pulled from stock");
        }
    }

    [Fact]
    public void OnlyTheGeneratorJoinsTheHullBeforeTheMainModule()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1).ToList();

        var hullJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.Hull));
        var mainModuleJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.MainModule));
        var generatorJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.Generator));

        Assert.True(hullJoinsIndex < mainModuleJoinsIndex);
        Assert.Equal(hullJoinsIndex, generatorJoinsIndex);

        var movementJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.MovementModule));
        Assert.True(mainModuleJoinsIndex < movementJoinsIndex, "movement module must not join the hull before the main module");
    }

    [Fact]
    public void MovementModuleJoinsOnlyAfterTheHullIsAlreadyPresent()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1).ToList();

        var hullJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.Hull));
        var movementJoinsIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains(Dxf1.MovementModule));

        Assert.True(hullJoinsIndex < movementJoinsIndex);
    }

    [Fact]
    public void SystemIsInstalledBeforeTheMainModuleIsAssembled()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1).ToList();

        var installIndex = lines.IndexOf($"INSTALL {Dxf1.System} {Dxf1.MainModule}");
        var mainModuleAssembledIndex = lines.FindIndex(l => l.StartsWith("ASSEMBLE") && l.Contains($"{Dxf1.MainModule}{{{Dxf1.System}}}"));

        Assert.True(installIndex >= 0);
        Assert.True(installIndex < mainModuleAssembledIndex);
    }

    [Fact]
    public void RepeatsTheFullSequenceForEachRequestedUnit()
    {
        var lines = AssemblyPlanner.BuildInstructions(Dxf1, 2).ToList();

        Assert.Equal(2, lines.Count(l => l == "PRODUCING DXF-1"));
        Assert.Equal(2, lines.Count(l => l == "FINISHED DXF-1"));
        Assert.Equal(2, lines.Count(l => l == $"GET_OUT_STOCK 1 {Dxf1.Hull}"));
    }
}
