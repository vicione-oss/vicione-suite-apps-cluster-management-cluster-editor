using System.Threading.Tasks;
using Bunit;
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
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.Services.AddPublishedConnectorsSectionContextMenu();
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.SetupPublishedConnectorsService();
        ctx.SetupDragService();

        // Act
        var component = ctx.Render<PublishedConnectorsSectionContextMenu>();

        // Assert
        Assert.NotNull(component);
    }
}
