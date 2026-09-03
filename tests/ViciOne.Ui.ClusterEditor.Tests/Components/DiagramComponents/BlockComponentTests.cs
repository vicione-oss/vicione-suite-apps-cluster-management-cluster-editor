using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams.Core.Geometry;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Components.DiagramComponents;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.PublishedConnectors.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;
using Bunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.DiagramComponents;

public class BlockComponentTests
{
    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

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

    [Fact]
    public async Task Double_click_on_a_container_published_marker_reveals_the_underlying_connector()
    {
        // Arrange
        using var ctx = CreateTestContext();
        using var builder = BuilderFactory.Create();
        var (containerNode, containerConnector, boundaryLink) = await SetupPublishedContainerConnectorAsync(ctx, builder);
        using var linkToDispose = boundaryLink;

        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var containerModel = datastore.DataflowDiagramMapping.GetModel(containerConnector);

        IConnector? requestedConnector = null;
        ctx.Services.GetRequiredService<PublishedConnectorsService>().PublishedConnectorSelectionRequested += c => requestedConnector = c;

        var component = RenderBlockComponent(ctx, containerNode);

        // Act
        await component.InvokeAsync(() => component.Find(".marker.publish").DoubleClick());

        // Assert - only the underlying connector has an entry in the section, the container level
        // proxy never appears there.
        requestedConnector.Should().BeSameAs(containerModel.GetUnderlyingConnector());
        requestedConnector.Should().NotBeSameAs(containerModel);
    }

    [Fact]
    public async Task Double_click_on_published_marker_with_links_does_not_request_the_published_connectors_section()
    {
        // Arrange
        using var ctx = CreateTestContext();
        using var builder = BuilderFactory.Create();
        var (node, connector) = await SetupPublishedConnectorAsync(ctx, builder);

        // A published connector that is linked keeps its existing "jump to the link target" behaviour.
        var blockNodeConnector = ctx.Services.GetRequiredService<IDatastore>().DataflowDiagramMapping.GetDiagramModel(connector);
        blockNodeConnector.PublishedConnectorMarker.Links.Add(new Link());

        DataflowToolbarSection? requestedSection = null;
        ctx.Services.GetRequiredService<ToolbarService>().DataflowToolbarSectionRequested += s => requestedSection = s;

        IConnector? requestedConnector = null;
        ctx.Services.GetRequiredService<PublishedConnectorsService>().PublishedConnectorSelectionRequested += c => requestedConnector = c;

        var component = RenderBlockComponent(ctx, node);

        // Act
        await component.InvokeAsync(() => component.Find(".marker.publish").DoubleClick());

        // Assert
        requestedSection.Should().BeNull();
        requestedConnector.Should().BeNull();
    }

    [Fact]
    public async Task Double_click_on_unlinked_published_marker_reveals_the_entry_in_the_published_connectors_section()
    {
        // Arrange
        using var ctx = CreateTestContext();
        using var builder = BuilderFactory.Create();
        var (node, connector) = await SetupPublishedConnectorAsync(ctx, builder);

        DataflowToolbarSection? requestedSection = null;
        ctx.Services.GetRequiredService<ToolbarService>().DataflowToolbarSectionRequested += s => requestedSection = s;

        IConnector? requestedConnector = null;
        ctx.Services.GetRequiredService<PublishedConnectorsService>().PublishedConnectorSelectionRequested += c => requestedConnector = c;

        var component = RenderBlockComponent(ctx, node);

        // Act
        await component.InvokeAsync(() => component.Find(".marker.publish").DoubleClick());

        // Assert
        requestedSection.Should().Be(DataflowToolbarSection.Connectors);
        requestedConnector.Should().BeSameAs(connector);
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
        ctx.SetupPublishedConnectorsService();
        ctx.Services.TryAddScoped(_ => Substitute.For<IPropertyGridController<DataflowToolbarPropertyGridContext>>());

        ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

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

    private static async Task<(FunctionBlockNode Node, IConnector Connector)> SetupPublishedConnectorAsync(Bunit.TestContext ctx, IClusterBuilder builder)
    {
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();

        await datastore.Load(builder, diagramService, Ct);

        var node = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(0, 0), Ct);
        var connector = datastore.DataflowDiagramMapping.GetModel(node).SystemOutputs.First();

        // Publish the connector on the diagram model directly: the marker state is what the
        // component renders, and going through the builder would only exercise the event buffer.
        datastore.DataflowDiagramMapping.GetDiagramModel(connector).SetPublished(true);

        return (node, connector);
    }

    // Links two function blocks, then moves the source into a new child container so the container
    // exposes the crossing connector, and publishes that connector.
    private static async Task<(ChildContainerNode Node, BlockNodeConnector Connector, BlockNodeLink Link)>
        SetupPublishedContainerConnectorAsync(Bunit.TestContext ctx, IClusterBuilder builder)
    {
        var datastore = ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = ctx.Services.GetRequiredService<DiagramService>();
        var selectionManager = ctx.Services.GetRequiredService<SelectionManager>();

        await datastore.Load(builder, diagramService, Ct);

        var movedNode = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(0, 0), Ct);
        var stayingNode = await datastore.AddFunctionBlock(diagramService, BuilderFactory.FbDesignId, new Point(300, 0), Ct);
        var movedFunctionBlock = datastore.DataflowDiagramMapping.GetModel(movedNode);
        var stayingFunctionBlock = datastore.DataflowDiagramMapping.GetModel(stayingNode);

        var sourcePort = datastore.DataflowDiagramMapping.GetDiagramModel(movedFunctionBlock.SystemOutputs.First());
        var targetPort = datastore.DataflowDiagramMapping.GetDiagramModel(stayingFunctionBlock.SystemInputs.First());
        var boundaryLink = new BlockNodeLink(sourcePort, targetPort);
        datastore.AddLink(boundaryLink);

        diagramService.Diagram!.Nodes.Add([movedNode, stayingNode]);
        await datastore.MoveToNewContainerAsync(diagramService, new Point(150, 150), [movedFunctionBlock], [], [], selectionManager);

        var container = datastore.ActiveContainer.Containers.Single();
        datastore.DataflowDiagramMapping.TryGetDiagramModel(container, out var containerNode);

        var containerConnector = containerNode!.ConnectorsToList().First(c => c.HasLink());
        containerConnector.SetPublished(true);

        return (containerNode, containerConnector, boundaryLink);
    }
}
