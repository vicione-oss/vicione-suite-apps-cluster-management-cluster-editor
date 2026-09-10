using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.SearchAndTools.Components;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.SearchAndTools.Components;

public class SearchAndToolsSectionContentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<LabelOrderService>();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped<TraceService>();
        ctx.SetupResizeObserver();

        ctx.CreateDiagramInstance();

        // Act
        var component = ctx.Render<SearchAndToolsSectionContent>();

        // Assert
        Assert.NotNull(component);
    }
}
