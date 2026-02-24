using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarMain;

public class MainToolbarTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<MainToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
