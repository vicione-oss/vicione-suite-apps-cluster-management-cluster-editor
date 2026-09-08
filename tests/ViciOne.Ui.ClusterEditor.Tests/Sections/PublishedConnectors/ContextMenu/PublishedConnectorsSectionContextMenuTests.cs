using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.PublishedConnectors.ContextMenu;

public class PublishedConnectorsSectionContextMenuTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupDragService();

        // Act
        var component = ctx.RenderComponent<PublishedConnectorsSectionContextMenu>();

        // Assert
        Assert.NotNull(component);
    }
}
