using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class ContainerRestructureServiceTests : IAsyncLifetime
{
    private const double Tolerance = 0.001;
    private readonly IClusterBuilder _builder;
    private readonly TestContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly ClusterEditService _editService;
    private readonly Guid _fbDesignId;
    private readonly List<BlockNodeLink> _linksToDispose = [];
    private readonly SelectionManager _selectionManager;
    private readonly DatastoreState _state;
    private readonly ContainerRestructureService _sut;

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public ContainerRestructureServiceTests()
    {
        _ctx = new TestContext();
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
        _selectionManager = _ctx.Services.GetRequiredService<SelectionManager>();
        _sut = _ctx.Services.GetRequiredService<ContainerRestructureService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    public ValueTask InitializeAsync()
        => new(_datastore.Load(_builder, _diagramService, CancellationToken.None));

    public async ValueTask DisposeAsync()
    {
        foreach (var link in _linksToDispose)
            link.Dispose();

        _builder.Dispose();
        _ctx.Dispose();
        await _sut.DisposeAsync();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
        _selectionManager.Dispose();
    }

    // --- DisposeAsync -----------------------------------------------------------------------------

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        await _sut.DisposeAsync();

        // Act
        var act = async () => await _sut.DisposeAsync();

        // Assert - the idempotency guard prevents disposing the semaphore twice
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhileDissolveOperationsArePending_CompletesAllGracefully()
    {
        // Arrange - many collapsed containers queued onto the single dissolve semaphore
        const int OperationCount = 50;
        var containers = new List<ChildContainer>(OperationCount);
        foreach (var i in Enumerable.Range(0, OperationCount))
        {
            var (_, functionBlock) = await AddFunctionBlockAsync(new Point(10, i));
            var (_, container) = await CreateCollapsedContainerAsync(new Point(100, i), functionBlocks: [functionBlock]);
            containers.Add(container);
        }

        // Act - dispose while the dissolve operations are in flight
        var tasks = containers
            .Select(container => _sut.DissolveContainerAsync(container, _diagramService, _selectionManager))
            .ToList();
        await _sut.DisposeAsync();
        var act = () => Task.WhenAll(tasks);

        // Assert - the OperationCanceled/ObjectDisposed guards swallow the teardown races, so nothing faults
        await act.Should().NotThrowAsync();
        tasks.Should().OnlyContain(task => task.IsCompletedSuccessfully);
    }

    // --- DissolveContainerAsync -------------------------------------------------------------------

    [Fact]
    public async Task DissolveContainerAsync_AfterDispose_ReturnsGracefullyWithoutDissolving()
    {
        // Arrange
        var (containerNode, container) = await CreateCollapsedContainerAsync(new Point(200, 200));
        PlaceOnDiagram(containerNode);
        await _sut.DisposeAsync();

        // Act
        var act = async () => await _sut.DissolveContainerAsync(container, _diagramService, _selectionManager);

        // Assert - the disposed guard short-circuits before any mutation happens
        await act.Should().NotThrowAsync();
        _state.DataflowDiagramMapping.TryGetDiagramModel(container, out _).Should().BeTrue();
        _diagramService.Diagram.Nodes.Should().Contain(containerNode);
    }

    [Fact]
    public async Task DissolveContainerAsync_CalledConcurrentlyForDifferentContainers_DissolvesAll()
    {
        // Arrange
        var containers = new List<(ChildContainerNode Node, ChildContainer Model)>();
        foreach (var i in Enumerable.Range(0, 5))
        {
            var (_, functionBlock) = await AddFunctionBlockAsync(new Point(10, i * 50));
            containers.Add(await CreateCollapsedContainerAsync(new Point(100, i * 50), functionBlocks: [functionBlock]));
        }

        // Act - the shared semaphore serializes the work without surfacing cancellation to callers
        var tasks = containers
            .Select(container => _sut.DissolveContainerAsync(container.Model, _diagramService, _selectionManager))
            .ToList();
        await Task.WhenAll(tasks);

        // Assert
        tasks.Should().OnlyContain(task => task.IsCompletedSuccessfully);
        foreach (var (_, model) in containers)
            _state.DataflowDiagramMapping.TryGetDiagramModel(model, out _).Should().BeFalse();
    }

    [Fact]
    public async Task DissolveContainerAsync_CalledConcurrentlyForSameContainer_DissolvesOnceAndCompletesAll()
    {
        // Arrange
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(50, 50));
        var (_, container) = await CreateCollapsedContainerAsync(new Point(100, 100), functionBlocks: [functionBlock]);

        // Act - only the first caller finds the container still mapped; the rest hit the guard and return
        var tasks = Enumerable
            .Range(0, 50)
            .Select(_ => _sut.DissolveContainerAsync(container, _diagramService, _selectionManager))
            .ToList();
        await Task.WhenAll(tasks);

        // Assert
        tasks.Should().OnlyContain(task => task.IsCompletedSuccessfully);
        _state.DataflowDiagramMapping.TryGetDiagramModel(container, out _).Should().BeFalse();
        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out var extractedNode).Should().BeTrue();
        functionBlock.Container.Should().BeSameAs(_state.ActiveContainer);
        _selectionManager.IsSelected(extractedNode!).Should().BeTrue();
    }

    [Fact]
    public async Task DissolveContainerAsync_ExtractsChildrenReparentsToActiveContainerAndSelectsThem()
    {
        // Arrange
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(10, 10));
        var (_, innerContainer) = await AddChildContainerAsync(new Point(40, 40));
        var (_, label) = AddLabel(new Point(70, 70));
        var (containerNode, container) = await CreateCollapsedContainerAsync(
            new Point(120, 120),
            functionBlocks: [functionBlock],
            containers: [innerContainer],
            labels: [label]);
        PlaceOnDiagram(containerNode);

        // Act
        await _sut.DissolveContainerAsync(container, _diagramService, _selectionManager);

        // Assert - the container is gone and every child is re-projected under the active container
        _diagramService.Diagram.Nodes.Should().NotContain(containerNode);
        _state.DataflowDiagramMapping.TryGetDiagramModel(container, out _).Should().BeFalse();

        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out var newFunctionBlockNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(innerContainer, out var newInnerContainerNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(label, out var newLabelNode).Should().BeTrue();

        functionBlock.Container.Should().BeSameAs(_state.ActiveContainer);
        innerContainer.Parent.Should().BeSameAs(_state.ActiveContainer);
        label.Container.Should().BeSameAs(_state.ActiveContainer);

        _selectionManager.IsSelected(newFunctionBlockNode!).Should().BeTrue();
        _selectionManager.IsSelected(newInnerContainerNode!).Should().BeTrue();
        _selectionManager.IsSelected(newLabelNode!).Should().BeTrue();
    }

    [Fact]
    public async Task DissolveContainerAsync_RemapsLinksCrossingFormerBoundary()
    {
        // Arrange - outerFb stays in the active container, innerFb is collapsed into a child container
        var (outerNode, outerFb) = await AddFunctionBlockAsync(new Point(50, 50));
        var (_, innerFb) = await AddFunctionBlockAsync(new Point(250, 50));
        var (link, originalLinkNode) = LinkFunctionBlocks(outerFb, innerFb);
        var (_, container) = await CreateCollapsedContainerAsync(new Point(150, 150), functionBlocks: [innerFb]);

        // Act
        await _sut.DissolveContainerAsync(container, _diagramService, _selectionManager);

        // Assert - the boundary link is rebuilt as a direct function-block-to-function-block link
        _state.DataflowDiagramMapping.ContainsMapping(link).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(link, out var newLinkNode).Should().BeTrue();
        _state.DataflowDiagramMapping.TryGetDiagramModel(innerFb, out var newInnerNode).Should().BeTrue();

        newLinkNode.Should().NotBeSameAs(originalLinkNode);
        newLinkNode.SourceNode.Should().BeSameAs(outerNode);
        newLinkNode.TargetNode.Should().BeSameAs(newInnerNode);
    }

    [Fact]
    public async Task DissolveContainerAsync_RepositionsExtractedChildAroundFormerContainerCenter()
    {
        // Arrange - a single extracted node must end up centered on the former container center
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(0, 0));
        var (_, container) = await CreateCollapsedContainerAsync(new Point(400, 300), functionBlocks: [functionBlock]);
        var expectedCenter = ContainerCenter(container);

        // Act
        await _sut.DissolveContainerAsync(container, _diagramService, _selectionManager);

        // Assert - relies on the mocked measureNameFieldHeights returning 0 for deterministic sizing
        var extractedNode = _state.DataflowDiagramMapping.GetDiagramModel(functionBlock);
        var extractedCenter = new List<NodeModel> { extractedNode }.GetBounds().Center;
        extractedCenter.X.Should().BeApproximately(expectedCenter.X, Tolerance);
        extractedCenter.Y.Should().BeApproximately(expectedCenter.Y, Tolerance);
    }

    [Fact]
    public async Task DissolveContainerAsync_WhenContainerNotMapped_ReturnsGracefully()
    {
        // Arrange
        var (_, container) = await CreateCollapsedContainerAsync(new Point(200, 200));
        _state.DataflowDiagramMapping.Remove(container);

        // Act
        var act = async () => await _sut.DissolveContainerAsync(container, _diagramService, _selectionManager);

        // Assert - the missing-mapping guard returns before touching the diagram
        await act.Should().NotThrowAsync();
        _selectionManager.HasSelection.Should().BeFalse();
    }

    // --- MoveToNewContainerAsync ------------------------------------------------------------------

    [Fact]
    public async Task MoveToNewContainerAsync_WithEmptySelection_CreatesAndSelectsEmptyContainer()
    {
        // Act
        await _sut.MoveToNewContainerAsync(_diagramService, new Point(200, 200), [], [], [], _selectionManager);

        // Assert
        var newContainer = _state.ActiveContainer.Containers.Should().ContainSingle().Subject;
        _state.DataflowDiagramMapping.TryGetDiagramModel(newContainer, out var newContainerNode).Should().BeTrue();
        _diagramService.Diagram.Nodes.Should().Contain(newContainerNode!);
        _selectionManager.IsSelected(newContainerNode!).Should().BeTrue();
    }

    [Fact]
    public async Task MoveToNewContainerAsync_WithFunctionBlocks_ReparentsRemovesOldNodesAndSelectsNewContainer()
    {
        // Arrange
        var (functionBlockNode1, functionBlock1) = await AddFunctionBlockAsync(new Point(10, 10));
        var (functionBlockNode2, functionBlock2) = await AddFunctionBlockAsync(new Point(40, 40));
        PlaceOnDiagram(functionBlockNode1, functionBlockNode2);

        // Act
        await _sut.MoveToNewContainerAsync(_diagramService, new Point(200, 200), [functionBlock1, functionBlock2], [], [], _selectionManager);

        // Assert
        var newContainer = _state.ActiveContainer.Containers.Should().ContainSingle().Subject;
        _state.DataflowDiagramMapping.TryGetDiagramModel(newContainer, out var newContainerNode).Should().BeTrue();

        _diagramService.Diagram.Nodes.Should().Contain(newContainerNode!);
        _diagramService.Diagram.Nodes.Should().NotContain(functionBlockNode1).And.NotContain(functionBlockNode2);

        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock1, out _).Should().BeFalse();
        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock2, out _).Should().BeFalse();
        functionBlock1.Container.Should().BeSameAs(newContainer);
        functionBlock2.Container.Should().BeSameAs(newContainer);

        _selectionManager.IsSelected(newContainerNode!).Should().BeTrue();
    }

    [Fact]
    public async Task MoveToNewContainerAsync_WithLinkCrossingBoundary_RemapsAffectedLink()
    {
        // Arrange - movedFb is linked to stayingFb; moving movedFb turns the link into a container boundary link
        var (_, movedFb) = await AddFunctionBlockAsync(new Point(50, 50));
        var (_, stayingFb) = await AddFunctionBlockAsync(new Point(300, 50));
        LinkFunctionBlocks(movedFb, stayingFb);

        // Act
        await _sut.MoveToNewContainerAsync(_diagramService, new Point(150, 150), [movedFb], [], [], _selectionManager);

        // Assert - the new container exposes exactly one connector carrying the rebuilt boundary link
        var newContainer = _state.ActiveContainer.Containers.Should().ContainSingle().Subject;
        _state.DataflowDiagramMapping.TryGetDiagramModel(newContainer, out var newContainerNode).Should().BeTrue();
        newContainerNode!.ConnectorsToList().Count(connector => connector.HasLink()).Should().Be(1);
    }

    [Fact]
    public async Task MoveToNewContainerAsync_WithMixedChildren_ReparentsContainersAndLabels()
    {
        // Arrange
        var (_, functionBlock) = await AddFunctionBlockAsync(new Point(10, 10));
        var (_, innerContainer) = await AddChildContainerAsync(new Point(40, 40));
        var (_, label) = AddLabel(new Point(70, 70));

        // Act
        await _sut.MoveToNewContainerAsync(_diagramService, new Point(200, 200), [functionBlock], [innerContainer], [label], _selectionManager);

        // Assert
        var newContainer = _state.ActiveContainer.Containers.Should().ContainSingle().Subject;
        _state.DataflowDiagramMapping.TryGetDiagramModel(newContainer, out var newContainerNode).Should().BeTrue();

        functionBlock.Container.Should().BeSameAs(newContainer);
        innerContainer.Parent.Should().BeSameAs(newContainer);
        label.Container.Should().BeSameAs(newContainer);

        _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _).Should().BeFalse();
        _state.DataflowDiagramMapping.TryGetDiagramModel(innerContainer, out _).Should().BeFalse();
        _state.DataflowDiagramMapping.TryGetDiagramModel(label, out _).Should().BeFalse();

        _selectionManager.IsSelected(newContainerNode!).Should().BeTrue();
    }

    // --- Helpers ----------------------------------------------------------------------------------

    private async Task<(ChildContainerNode Node, ChildContainer Model)> AddChildContainerAsync(Point position, params IContainerChild[] children)
    {
        var node = await _editService.AddChildContainer(_diagramService, position, Ct, children);
        return (node, _state.DataflowDiagramMapping.GetModel(node));
    }

    private async Task<(FunctionBlockNode Node, FunctionBlock Model)> AddFunctionBlockAsync(Point position)
    {
        var node = await _editService.AddFunctionBlock(_diagramService, _fbDesignId, position, Ct);
        return (node, _state.DataflowDiagramMapping.GetModel(node));
    }

    private (LabelNode Node, Label Model) AddLabel(Point position)
    {
        var node = _editService.AddLabel(position, 0);
        return (node, _state.DataflowDiagramMapping.GetModel(node));
    }

    private static Point ContainerCenter(ChildContainer container)
        => new(
            (container.X ?? 0) + (BlockNodeLayout.Width / 2.0),
            (container.Y ?? 0) + (BlockNodeLayout.RowHeight * (BlockNodeLayout.SystemConnectorRows + BlockNodeLayout.MinimumConnectorRows) / 2)
        );

    /// <summary>
    /// Creates a child container that owns the supplied children and removes those children from the
    /// mapping, reproducing the post-grouping state in which only the collapsed container node remains
    /// mapped in the active container.
    /// </summary>
    private async Task<(ChildContainerNode Node, ChildContainer Model)> CreateCollapsedContainerAsync(
        Point position,
        IReadOnlyList<FunctionBlock>? functionBlocks = null,
        IReadOnlyList<ChildContainer>? containers = null,
        IReadOnlyList<Label>? labels = null)
    {
        functionBlocks ??= [];
        containers ??= [];
        labels ??= [];

        IContainerChild[] children = [.. functionBlocks.Cast<IContainerChild>(), .. containers, .. labels];
        var node = await _editService.AddChildContainer(_diagramService, position, Ct, children);
        var model = _state.DataflowDiagramMapping.GetModel(node);

        foreach (var functionBlock in functionBlocks)
            _state.DataflowDiagramMapping.Remove(functionBlock);
        foreach (var container in containers)
            _state.DataflowDiagramMapping.Remove(container);
        foreach (var label in labels)
            _state.DataflowDiagramMapping.Remove(label);

        return (node, model);
    }

    private (Link Model, BlockNodeLink Node) LinkFunctionBlocks(FunctionBlock source, FunctionBlock target)
    {
        var link = _builder.Editors.Connector.AddLink(source.SystemOutputs.First(), target.SystemInputs.First());
        var node = LinkMapper.CreateLink(_state, link);
        _state.DataflowDiagramMapping.Add(link, node);
        _linksToDispose.Add(node);
        return (link, node);
    }

    private void PlaceOnDiagram(params NodeModel[] nodes)
        => _diagramService.Diagram.Nodes.Add(nodes);
}
