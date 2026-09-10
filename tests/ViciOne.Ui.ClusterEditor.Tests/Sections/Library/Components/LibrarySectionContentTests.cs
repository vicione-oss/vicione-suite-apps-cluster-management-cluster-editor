using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Sections.Library.Components;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Library.Components;

public class LibrarySectionContentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupTreeEditorJs();
        ctx.SetupClusterEditorManagement();
        ctx.SetupResizeObserver();
        ctx.Services.AddScoped<InputEventService>();

        // Act
        var component = ctx.Render<LibrarySectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
