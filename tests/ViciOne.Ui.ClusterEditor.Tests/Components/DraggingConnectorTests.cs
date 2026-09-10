using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class DraggingConnectorTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDatastore();
        ctx.SetupDragService();
        ctx.SetupDiagramService();

        // Act
        var component = ctx.Render<DraggingConnector>();

        // Assert
        Assert.NotNull(component);
    }
}
