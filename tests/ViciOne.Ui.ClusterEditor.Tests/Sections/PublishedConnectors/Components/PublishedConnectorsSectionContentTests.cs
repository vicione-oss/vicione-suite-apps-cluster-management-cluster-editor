using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Components;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.Components;

public class PublishedConnectorsSectionContentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDiagramService();
        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupSelectionManager();
        ctx.SetupConnectorService();
        ctx.SetupDragService();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<PublishedConnectorsSectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
