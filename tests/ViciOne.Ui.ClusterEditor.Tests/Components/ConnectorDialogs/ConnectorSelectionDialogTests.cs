using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Components.ConnectorDialogs;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ConnectorDialogs;

public class ConnectorSelectionDialogTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuSettings>());
        ctx.SetupConnectorSelectionDialogService();
        ctx.SetupConnectorService();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<ConnectorSelectionDialog>();

        // Assert
        Assert.NotNull(component);
    }
}
