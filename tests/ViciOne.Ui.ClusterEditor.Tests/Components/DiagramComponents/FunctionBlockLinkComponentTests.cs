using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class FunctionBlockLinkComponentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();
        ctx.SetupSelectionManager();
        ctx.CreateDiagramInstance();

        using var blockNodeLink = ctx.CreateBlockNodeLink();

        // Act
        var component = ctx.Render<BlockLinkComponent>(parameters => parameters
            .Add(p => p.Link, blockNodeLink));

        // Assert
        Assert.NotNull(component);
    }
}
