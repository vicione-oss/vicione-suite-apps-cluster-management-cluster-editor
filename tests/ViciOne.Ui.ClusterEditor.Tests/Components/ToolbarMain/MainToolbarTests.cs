using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.Blazor.Components.Toolbar.Components;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain;
using ViciOne.Ui.ClusterEditor.Components.ToolbarMain.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ToolbarMain;

public class MainToolbarTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();

        ctx.ComponentFactories.AddStub<Toolbar>();
        ctx.Services.AddMainToolbar();

        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<FullscreenService>();
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<MainToolbar>();

        // Assert
        Assert.NotNull(component);
    }
}
