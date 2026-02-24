using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class DraggingConnectorTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupDragService();
        ctx.SetupDiagramService();

        // Act
        var component = ctx.RenderComponent<DraggingConnector>();

        // Assert
        Assert.NotNull(component);
    }
}
