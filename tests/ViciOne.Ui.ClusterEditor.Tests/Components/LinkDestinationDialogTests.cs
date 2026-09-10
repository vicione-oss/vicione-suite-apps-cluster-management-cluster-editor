using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class LinkDestinationDialogTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.Services.AddDialog();
        ctx.SetupLinkDestinationDialogService();
        ctx.SetupConnectorService();
        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<LinkDestinationDialog>();

        // Assert
        Assert.NotNull(component);
    }
}
