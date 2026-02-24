using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.SearchAndTools.Components;

public class SearchAndToolsSectionContentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDevExpressBlazor();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<LabelOrderService>();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped<TraceService>();
        ctx.SetupResizeObserver();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.RenderComponent<SearchAndToolsSectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
