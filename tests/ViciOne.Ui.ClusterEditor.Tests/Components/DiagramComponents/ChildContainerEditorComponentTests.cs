using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class ChildContainerEditorComponentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<TooltipService>();
        ctx.SetupBoundsService();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;

        // Act
        var component = ctx.Render<ChildContainerEditorComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, new ChildContainerNode()));

        // Assert
        Assert.NotNull(component);
    }
}
