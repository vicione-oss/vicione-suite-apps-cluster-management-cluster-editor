using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class MinimapTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;

        // Act
        var component = ctx.RenderComponent<Minimap>(parameters => parameters
            .Add(p => p.Diagram, diagram));

        // Assert
        Assert.NotNull(component);
    }
}
