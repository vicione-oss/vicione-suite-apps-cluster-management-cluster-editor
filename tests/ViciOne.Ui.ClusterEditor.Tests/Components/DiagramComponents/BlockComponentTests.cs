using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class BlockComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = new Bunit.TestContext();
        ctx.Services.AddBlockNodeConnectorContextMenu();
        ctx.SetupDataPortTreeAdapter();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());
        ctx.Services.TryAddScoped(_ => Substitute.For<IDataManagementService>());
        ctx.SetupLinkDestinationDialogService();
        ctx.SetupDiagramService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IFbSettingsEditorRequest>());
        ctx.Services.AddNodeEditorContextMenu();
        ctx.SetupSelectionManager();
        ctx.Services.TryAddScoped<ToolbarService>();
        ctx.Services.TryAddScoped<TooltipService>();
        ctx.SetupConnectorService();
        ctx.SetupBoundsService();
        ctx.SetupDragService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IPropertyGridController<DataflowToolbarPropertyGridContext>>());

        ctx.CreateDiagramInstance();
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;
        var childContainerNode = new ChildContainerNode();

        ctx.Services.GetRequiredService<IDatastore>().DataflowDiagramMapping.Add(new(), childContainerNode);

        // Act
        var component = ctx.RenderComponent<BlockComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, childContainerNode));

        // Assert
        Assert.NotNull(component);
    }
}
