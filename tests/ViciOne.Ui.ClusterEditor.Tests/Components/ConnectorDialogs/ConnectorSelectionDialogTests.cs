using System.Threading.Tasks;
using Bunit;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ConnectorDialogs;

public class ConnectorSelectionDialogTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.Services.AddDialog();
        ctx.SetupConnectorSelectionDialogService();
        ctx.SetupConnectorService();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<ConnectorSelectionDialog>();

        // Assert
        Assert.NotNull(component);
    }
}
