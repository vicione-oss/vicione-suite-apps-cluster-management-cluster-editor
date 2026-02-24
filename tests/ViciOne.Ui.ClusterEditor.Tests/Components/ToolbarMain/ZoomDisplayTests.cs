using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarMain;

public class ZoomDisplayTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.Services.TryAddScoped<DiagramEventService>();
        ctx.SetupDiagramService();

        // Act
        var component = ctx.RenderComponent<ZoomDisplay>();

        // Assert
        Assert.NotNull(component);
    }
}
