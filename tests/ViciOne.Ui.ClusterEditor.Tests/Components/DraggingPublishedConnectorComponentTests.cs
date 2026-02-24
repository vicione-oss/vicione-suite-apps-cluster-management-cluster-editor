using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class DraggingPublishedConnectorComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupDragService();

        // Act
        var component = ctx.RenderComponent<DraggingPublishedConnectorComponent>();

        // Assert
        Assert.NotNull(component);
    }
}
