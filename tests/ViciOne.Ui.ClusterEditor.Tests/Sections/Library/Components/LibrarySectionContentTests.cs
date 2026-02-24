using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Sections.Library.Components;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Library.Components;

public class LibrarySectionContentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupTreeEditorJs();
        ctx.SetupDataManagementService();
        ctx.SetupResizeObserver();
        ctx.Services.AddScoped<InputEventService>();

        // Act
        var component = ctx.RenderComponent<LibrarySectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
