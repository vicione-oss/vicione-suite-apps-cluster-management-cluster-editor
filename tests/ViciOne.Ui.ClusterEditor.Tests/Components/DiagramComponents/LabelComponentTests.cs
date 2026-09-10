using System.Threading.Tasks;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class LabelComponentTests
{
    [Fact]
    public async Task Component_should_render()
    {
        // Arrange
        await using var ctx = new BunitContext();
        ctx.SetupDiagramService();
        ctx.Services.AddNodeEditorContextMenu();
        ctx.SetupSelectionManager();

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;
        var labelNode = new LabelNode(new Point(0, 0));

        ctx.Services.GetRequiredService<IDatastore>().DataflowDiagramMapping.Add(new(), labelNode);

        // Act
        var component = ctx.Render<LabelComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, labelNode));

        // Assert
        Assert.NotNull(component);
    }
}
