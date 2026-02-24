using Blazor.Diagrams.Core.Geometry;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class LabelComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.SetupDiagramService();
        ctx.Services.AddNodeEditorContextMenu();
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;
        var labelNode = new LabelNode(new Point(0, 0));

        ctx.Services.GetRequiredService<Datastore>().DataflowDiagramMapping.Add(new(), labelNode);

        // Act
        var component = ctx.RenderComponent<LabelComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, labelNode));

        // Assert
        Assert.NotNull(component);
    }
}
