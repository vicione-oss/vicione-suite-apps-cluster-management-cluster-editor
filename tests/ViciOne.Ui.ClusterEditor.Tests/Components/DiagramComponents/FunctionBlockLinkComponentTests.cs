using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class FunctionBlockLinkComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();
        ctx.SetupSelectionManager();

        using var blockNodeLink = ctx.CreateBlockNodeLink();

        // Act
        var component = ctx.RenderComponent<BlockLinkComponent>(parameters => parameters
            .Add(p => p.Link, blockNodeLink));

        // Assert
        Assert.NotNull(component);
    }
}
