using Microsoft.AspNetCore.Components.Web;
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
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using Bunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class BlockComponentTests
{
    [Fact]
    public void Component_should_render()
    {
        // Arrange
        using var ctx = CreateTestContext();
        var childContainerNode = CreateMappedChildContainerNode(ctx);

        // Act
        var component = RenderBlockComponent(ctx, childContainerNode);

        // Assert
        Assert.NotNull(component);
    }

    [Theory]
    [InlineData(".input-connector-container")]
    [InlineData(".output-connector-container")]
    public void Connector_container_should_accept_a_drop(string containerSelector)
    {
        // Arrange
        using var ctx = CreateTestContext();
        var childContainerNode = CreateMappedChildContainerNode(ctx);

        using var inputConnector = ctx.CreateBlockNodeConnector(childContainerNode, isInput: true);
        using var outputConnector = ctx.CreateBlockNodeConnector(childContainerNode, isInput: false);
        AddConnectorRow(childContainerNode, inputConnector, outputConnector);

        var component = RenderBlockComponent(ctx, childContainerNode);

        // Act
        var exception = Record.Exception(() => component.Find(containerSelector).TriggerEvent("ondrop", new DragEventArgs()));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(".input-connector-container", ".input-port-container")]
    [InlineData(".output-connector-container", ".output-port-container")]
    public void Connector_container_should_allow_drag_over_for_a_valid_drop_target(string containerSelector, string portSelector)
    {
        // Arrange
        using var ctx = CreateTestContext();
        var childContainerNode = CreateMappedChildContainerNode(ctx);

        using var inputConnector = ctx.CreateBlockNodeConnector(childContainerNode, isInput: true);
        using var outputConnector = ctx.CreateBlockNodeConnector(childContainerNode, isInput: false);
        AddConnectorRow(childContainerNode, inputConnector, outputConnector);

        inputConnector.SetIsValidDropTarget(true);
        outputConnector.SetIsValidDropTarget(true);

        // Act
        var component = RenderBlockComponent(ctx, childContainerNode);

        // Assert
        Assert.Equal("event.preventDefault();", component.Find(containerSelector).GetAttribute("ondragover"));
        Assert.False(component.Find(portSelector).HasAttribute("ondragover"));
    }

    private static void AddConnectorRow(BlockNode node, BlockNodeConnector inputConnector, BlockNodeConnector outputConnector)
    {
        node.Connectors.Add([inputConnector, outputConnector]);
        node.InvalidateConnectorsCache();
    }

    private static ChildContainerNode CreateMappedChildContainerNode(Bunit.TestContext ctx)
    {
        var childContainerNode = new ChildContainerNode();
        ctx.Services.GetRequiredService<IDatastore>().DataflowDiagramMapping.Add(new(), childContainerNode);

        return childContainerNode;
    }

    private static Bunit.TestContext CreateTestContext()
    {
        var ctx = new Bunit.TestContext();
        ctx.Services.AddBlockNodeConnectorContextMenu();
        ctx.SetupDataPortTreeAdapter();
        ctx.Services.TryAddScoped(_ => Substitute.For<IRulesetProvider>());
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

        return ctx;
    }

    private static IRenderedComponent<BlockComponent> RenderBlockComponent(Bunit.TestContext ctx, BlockNode node)
    {
        var diagram = ctx.Services.GetRequiredService<DiagramService>().Diagram;

        return ctx.RenderComponent<BlockComponent>(parameters => parameters
            .Add(p => p.Diagram, diagram)
            .Add(p => p.Node, node));
    }
}
