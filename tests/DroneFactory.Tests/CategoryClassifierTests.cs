using DroneFactory.Domain;
using DroneFactory.Domain.Categories;

namespace DroneFactory.Tests;

public class CategoryClassifierTests
{
    [Theory]
    [InlineData("DXF-1", DroneCategory.Aerien)]
    [InlineData("RDL-1", DroneCategory.Terrestre)]
    [InlineData("WDS-1", DroneCategory.Submersible)]
    [InlineData("DYM-1", DroneCategory.Marin)]
    public void ClassifiesEveryBuiltInTemplate(string droneName, DroneCategory expected)
    {
        var template = DroneCatalog.Find(droneName)!;

        Assert.Equal(expected, CategoryClassifier.Classify(template));
    }

    [Fact]
    public void EveryBuiltInTemplateBelongsToAtLeastOneCategory()
    {
        foreach (var template in DroneCatalog.All)
        {
            Assert.NotEqual(DroneCategory.None, CategoryClassifier.Classify(template));
        }
    }

    [Fact]
    public void ATemplateMatchingNoRuleClassifiesAsNone()
    {
        // Hull/generator/movement module/system combination satisfying none of the four rules:
        // no (S) hull, a movement module tagged only (S) (neither F, M nor L), and a (2D) system.
        var template = new DroneTemplate("GhostDrone", "Hull_HF1", "Core_CG1", "Generator_GF1", "Move_MS1", "Processor_PG1", "System_SG1");

        Assert.Equal(DroneCategory.None, CategoryClassifier.Classify(template));
    }
}
