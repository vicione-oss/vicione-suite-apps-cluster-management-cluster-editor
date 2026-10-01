using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class LinkQueryServiceTests : IAsyncLifetime
{
    private readonly IClusterBuilder _builder;
    private readonly BunitContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly ClusterEditService _editService;
    private readonly Guid _fbDesignId;
    private readonly List<BlockNodeLink> _linksToDispose = [];
    private readonly ContainerRestructureService _restructureService;
    private readonly SelectionManager _selectionManager;
    private readonly DatastoreState _state;
    private readonly LinkQueryService _sut;

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public LinkQueryServiceTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        _ctx.Services.AddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        _ctx.Services.AddScoped<DiagramEventService>();
        _ctx.Services.AddScoped<ClusterBuilderEventBuffer>();
        _ctx.Services.AddDatastore();
        _ctx.Services.AddScoped<DiagramService>();
        _ctx.Services.AddScoped<SelectionManager>();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _diagramService.Diagram = new BlazorDiagram();
        _state = _ctx.Services.GetRequiredService<DatastoreState>();
        _editService = _ctx.Services.GetRequiredService<ClusterEditService>();
        _restructureService = _ctx.Services.GetRequiredService<ContainerRestructureService>();
        _selectionManager = _ctx.Services.GetRequiredService<SelectionManager>();
        _sut = _ctx.Services.GetRequiredService<LinkQueryService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    private async Task<(FunctionBlockNode Node, FunctionBlock Model)> AddFunctionBlockAsync(Point position)
    {
        var node = await _editService.AddFunctionBlock(_diagramService, _fbDesignId, position, Ct);
        return (node, _state.DataflowDiagramMapping.GetModel(node));
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var link in _linksToDispose)
            link.Dispose();

        _builder.Dispose();
        await _ctx.DisposeAsync();
        await _restructureService.DisposeAsync();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
        _selectionManager.Dispose();
    }

    public ValueTask InitializeAsync()
        => new(_datastore.Load(_builder, _diagramService, CancellationToken.None));

    private (Link Model, BlockNodeLink Node) LinkFunctionBlocks(FunctionBlock source, FunctionBlock target)
    {
        var link = _builder.Editors.Connector.AddLink(source.SystemOutputs.First(), target.SystemInputs.First());
        var node = LinkMapper.CreateLink(_state, link);
        _state.DataflowDiagramMapping.Add(link, node);
        _linksToDispose.Add(node);
        return (link, node);
    }

    /// <summary>
    /// Links two function blocks in the active container, then moves the source into a new child
    /// container so the link crosses the container boundary and is exposed through a container
    /// connector. Returns the block that stayed, the new container node, and the crossing link.
    /// </summary>
    private async Task<(FunctionBlockNode StayingNode, ChildContainerNode ContainerNode, Link BoundaryLink)> MoveFunctionBlockAcrossNewContainerBoundaryAsync()
    {
        var (movedNode, movedFb) = await AddFunctionBlockAsync(new Point(50, 50));
        var (stayingNode, stayingFb) = await AddFunctionBlockAsync(new Point(300, 50));
        var (boundaryLink, _) = LinkFunctionBlocks(movedFb, stayingFb);
        _diagramService.Diagram.Nodes.Add([movedNode, stayingNode]);

        await _restructureService.MoveToNewContainerAsync(_diagramService, new Point(150, 150), [movedFb], [], [], _selectionManager);

        var container = _state.ActiveContainer.Containers.Single();
        _state.DataflowDiagramMapping.TryGetDiagramModel(container, out var containerNode);
        return (stayingNode, containerNode!, boundaryLink);
    }

    // --- GetConnectedLinks ------------------------------------------------------------------------

    [Fact]
    public async Task GetConnectedLinks_ChildContainerNodeWithBoundaryLink_ReturnsCrossingLink()
    {
        // Arrange - moving a linked block into a new container turns the link into a boundary link
        // that the container exposes through a connector.
        var (_, containerNode, boundaryLink) = await MoveFunctionBlockAcrossNewContainerBoundaryAsync();

        // Act
        var result = _sut.GetConnectedLinks(containerNode, ConnectionDirection.Successor, depth: null);

        // Assert
        result.Should().ContainSingle().Which.Should().BeSameAs(boundaryLink);
    }

    [Fact]
    public async Task GetConnectedLinks_ChildContainerNodeWithoutConnectors_ReturnsEmpty()
    {
        // Arrange - an empty container has no connectors to walk.
        var containerNode = await _editService.AddChildContainer(_diagramService, new Point(50, 50), Ct);

        // Act
        var result = _sut.GetConnectedLinks(containerNode, ConnectionDirection.Successor, depth: null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConnectedLinks_FunctionBlockNodeWithDepthLimit_ReturnsOnlyLinksWithinDepth()
    {
        // Arrange - chain A -> B -> C
        var (aNode, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, b) = await AddFunctionBlockAsync(new Point(200, 0));
        var (_, c) = await AddFunctionBlockAsync(new Point(400, 0));
        var (abLink, _) = LinkFunctionBlocks(a, b);
        LinkFunctionBlocks(b, c);

        // Act - a depth of 1 stops after the direct successor link
        var result = _sut.GetConnectedLinks(aNode, ConnectionDirection.Successor, depth: 1);

        // Assert
        result.Should().ContainSingle().Which.Should().BeSameAs(abLink);
    }

    [Theory]
    [InlineData(ConnectionDirection.Successor)]
    [InlineData(ConnectionDirection.Predecessor)]
    public async Task GetConnectedLinks_FunctionBlockNodeWithoutDepthLimit_ReturnsAllLinksInDirection(ConnectionDirection direction)
    {
        // Arrange - chain A -> B -> C
        var (aNode, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, b) = await AddFunctionBlockAsync(new Point(200, 0));
        var (cNode, c) = await AddFunctionBlockAsync(new Point(400, 0));
        var (abLink, _) = LinkFunctionBlocks(a, b);
        var (bcLink, _) = LinkFunctionBlocks(b, c);
        var start = direction == ConnectionDirection.Successor ? aNode : cNode;

        // Act - the whole chain is reachable because every block stays in the active container
        var result = _sut.GetConnectedLinks(start, direction, depth: null);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(abLink);
        result.Should().Contain(bcLink);
    }

    [Fact]
    public async Task GetConnectedLinks_FunctionBlockNodeWithoutLinks_ReturnsEmpty()
    {
        // Arrange
        var (aNode, _) = await AddFunctionBlockAsync(new Point(0, 0));

        // Act
        var result = _sut.GetConnectedLinks(aNode, ConnectionDirection.Successor, depth: null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetConnectedLinks_UnsupportedBlockNodeType_ReturnsEmpty()
    {
        // Act - only function block and child container nodes are supported
        var result = _sut.GetConnectedLinks(new UnsupportedBlockNode(), ConnectionDirection.Successor, depth: null);

        // Assert
        result.Should().BeEmpty();
    }

    // --- GetConnectedNodeLinks --------------------------------------------------------------------

    [Fact]
    public async Task GetConnectedNodeLinks_FunctionBlockNode_ReturnsMappedNodeLinks()
    {
        // Arrange
        var (aNode, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, b) = await AddFunctionBlockAsync(new Point(200, 0));
        var (_, abNode) = LinkFunctionBlocks(a, b);

        // Act
        var result = _sut.GetConnectedNodeLinks(aNode, ConnectionDirection.Successor, depth: null);

        // Assert - the model link is projected back to its mapped diagram link
        result.Should().ContainSingle().Which.Should().BeSameAs(abNode);
    }

    [Fact]
    public async Task GetConnectedNodeLinks_MultipleNodes_ReturnsDistinctLinksFromBothDirections()
    {
        // Arrange - chain A -> B -> C
        var (aNode, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (bNode, b) = await AddFunctionBlockAsync(new Point(200, 0));
        var (_, c) = await AddFunctionBlockAsync(new Point(400, 0));
        var (_, abNode) = LinkFunctionBlocks(a, b);
        var (_, bcNode) = LinkFunctionBlocks(b, c);

        // Act - A and B both touch the A->B link, which must be reported only once
        var result = _sut.GetConnectedNodeLinks([aNode, bNode]).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(abNode);
        result.Should().Contain(bcNode);
    }

    // --- GetConnectedNodes ------------------------------------------------------------------------

    [Fact]
    public async Task GetConnectedNodes_ContainerNode_ReturnsFunctionBlockOnOtherSideOfBoundary()
    {
        // Arrange
        var (stayingNode, containerNode, _) = await MoveFunctionBlockAcrossNewContainerBoundaryAsync();

        // Act - walking out from the container connector reaches the block still in the active container
        var result = _sut.GetConnectedNodes([containerNode]);

        // Assert
        result.Should().ContainSingle().Which.Should().BeSameAs(stayingNode);
    }

    [Fact]
    public async Task GetConnectedNodes_FunctionBlockLinkedAcrossContainerBoundary_ReturnsContainerNode()
    {
        // Arrange
        var (stayingNode, containerNode, _) = await MoveFunctionBlockAcrossNewContainerBoundaryAsync();

        // Act - the moved peer lives inside the container, so it resolves to the container node
        var result = _sut.GetConnectedNodes([stayingNode]);

        // Assert
        result.Should().ContainSingle().Which.Should().BeSameAs(containerNode);
    }

    [Fact]
    public async Task GetConnectedNodes_LinkedFunctionBlocks_ReturnsConnectedFunctionBlockNodes()
    {
        // Arrange
        var (aNode, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (bNode, b) = await AddFunctionBlockAsync(new Point(200, 0));
        LinkFunctionBlocks(a, b);

        // Act & Assert - each endpoint resolves to the block on the other side of the link
        _sut.GetConnectedNodes([aNode]).Should().ContainSingle().Which.Should().BeSameAs(bNode);
        _sut.GetConnectedNodes([bNode]).Should().ContainSingle().Which.Should().BeSameAs(aNode);
    }

    [Fact]
    public async Task GetConnectedNodes_NodesWithoutLinks_ReturnsEmpty()
    {
        // Arrange
        var (aNode, _) = await AddFunctionBlockAsync(new Point(0, 0));
        var (bNode, _) = await AddFunctionBlockAsync(new Point(200, 0));

        // Act
        var result = _sut.GetConnectedNodes([aNode, bNode]);

        // Assert
        result.Should().BeEmpty();
    }

    // --- GetValidTargetConnectors -----------------------------------------------------------------

    [Fact]
    public void GetValidTargetConnectors_EmptyConnectorCollection_ReturnsEmpty()
    {
        // Act - an empty connector set has no direction to derive, so there are no valid targets
        var result = _sut.GetValidTargetConnectors([], visibleLink: false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValidTargetConnectors_InputSource_ReturnsLinkableOutputConnectors()
    {
        // Arrange
        var (_, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, b) = await AddFunctionBlockAsync(new Point(300, 0));
        var bOutputNode = _state.DataflowDiagramMapping.GetDiagramModel(b.SystemOutputs.First());

        // Act
        var result = _sut.GetValidTargetConnectors([a.SystemInputs.First()], visibleLink: false).ToList();

        // Assert - an input source only yields output connectors, and the compatible peer output qualifies
        result.Should().BeSubsetOf(_state.DataflowDiagramMapping.GetOutputNodeConnectors());
        result.Should().Contain(bOutputNode);
    }

    [Fact]
    public async Task GetValidTargetConnectors_OutputSource_ReturnsLinkableInputConnectors()
    {
        // Arrange
        var (_, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, b) = await AddFunctionBlockAsync(new Point(300, 0));
        var bInputNode = _state.DataflowDiagramMapping.GetDiagramModel(b.SystemInputs.First());

        // Act
        var result = _sut.GetValidTargetConnectors([a.SystemOutputs.First()], visibleLink: false).ToList();

        // Assert - an output source only yields input connectors, and the compatible peer input qualifies
        result.Should().BeSubsetOf(_state.DataflowDiagramMapping.GetInputNodeConnectors());
        result.Should().Contain(bInputNode);
    }

    // --- GetVisibleConnectorModel(s) --------------------------------------------------------------

    [Fact]
    public async Task GetVisibleConnectorModel_ChildContainerNodeConnector_ReturnsUpstreamContainerConnector()
    {
        // Arrange
        var (_, containerNode, _) = await MoveFunctionBlockAcrossNewContainerBoundaryAsync();
        var connectorNode = containerNode.ConnectorsToList().First(c => c.HasLink());
        var expected = _state.DataflowDiagramMapping.GetModel(connectorNode);

        // Act - a container connector resolves to its upstream connector inside the active container
        var result = _sut.GetVisibleConnectorModel(connectorNode);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetVisibleConnectorModel_FunctionBlockNodeConnector_ReturnsMappedModel()
    {
        // Arrange
        var (_, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var connector = a.SystemOutputs.First();
        var connectorNode = _state.DataflowDiagramMapping.GetDiagramModel(connector);

        // Act - a plain function block connector maps straight back to its model
        var result = _sut.GetVisibleConnectorModel(connectorNode);

        // Assert
        result.Should().BeSameAs(connector);
    }

    [Fact]
    public async Task GetVisibleConnectorModels_MultipleConnectors_ReturnsModelForEach()
    {
        // Arrange
        var (_, a) = await AddFunctionBlockAsync(new Point(0, 0));
        var inputConnector = a.SystemInputs.First();
        var outputConnector = a.SystemOutputs.First();
        var inputNode = _state.DataflowDiagramMapping.GetDiagramModel(inputConnector);
        var outputNode = _state.DataflowDiagramMapping.GetDiagramModel(outputConnector);

        // Act
        var result = _sut.GetVisibleConnectorModels([inputNode, outputNode]);

        // Assert - each connector is projected in order
        result.Should().Equal(inputConnector, outputConnector);
    }

    private sealed class UnsupportedBlockNode : BlockNode;
}
