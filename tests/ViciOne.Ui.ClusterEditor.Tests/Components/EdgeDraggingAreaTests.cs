using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components;

public class EdgeDraggingAreaTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDatastore();
        ctx.SetupDiagramService();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;

        // Act
        var component = ctx.Render<Grid>(parameters => parameters
            .Add(p => p.Diagram, diagram));

        // Assert
        Assert.NotNull(component);
    }
}
