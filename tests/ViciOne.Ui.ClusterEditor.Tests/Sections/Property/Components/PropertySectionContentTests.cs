using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Components;
using ViciOne.Ui.ClusterEditor.Sections.Property.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.Property.Components;

public class PropertySectionContentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDatastore();
        ctx.SetupSelectionManager();
        ctx.SetupResizeObserver();
        ctx.Services.TryAddScoped<InputEventService>();
        ctx.Services.AddPropertySection();
        ctx.Services.AddPropertyGrid<object>();

        ctx.CreateDiagramInstance();

        var propertyGridController = ctx.Services.GetRequiredService<IPropertyGridController<object>>();

        // Act
        var component = ctx.Render<PropertySectionContent<object>>(
            b => b.Add(p => p.PropertyGridController, propertyGridController));

        // Assert
        Assert.NotNull(component);
    }
}
