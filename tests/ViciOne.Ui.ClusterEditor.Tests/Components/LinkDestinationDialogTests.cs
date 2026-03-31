using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Extensions;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class LinkDestinationDialogTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.Services.TryAddScoped(_ => Substitute.For<IContextMenuSettings>());
        ctx.Services.AddDialog();
        ctx.SetupLinkDestinationDialogService();
        ctx.SetupConnectorService();

        // Act
        var component = ctx.RenderComponent<LinkDestinationDialog>();

        // Assert
        Assert.NotNull(component);
    }
}
