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
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped<TooltipService>();
        ctx.SetupBoundsService();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;

        // Act
        var component = ctx.RenderComponent<ChildContainerEditorComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, new ChildContainerNode()));

        // Assert
        Assert.NotNull(component);
    }
}
