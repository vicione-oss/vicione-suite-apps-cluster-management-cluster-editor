using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortDragCoordinatorTests : IAsyncDisposable
{
    private readonly ITreeBuilder _builder;
    private readonly DataPortDragCoordinator _coordinator;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly DragService _dragService;

    public DataPortDragCoordinatorTests()
    {
        _datastore = Substitute.For<IDatastore>();
        _diagramService = new DiagramService(
            _datastore,
            new DiagramEventService(Substitute.For<ILogger<DiagramEventService>>()),
            Substitute.For<ILogger<DiagramService>>())
        {
            Diagram = new global::Blazor.Diagrams.BlazorDiagram()
        };
        _dragService = new DragService(
            new DiagramEventService(Substitute.For<ILogger<DiagramEventService>>()),
            _diagramService,
            new InputEventService());
        _builder = Substitute.For<ITreeBuilder>();
        _coordinator = new DataPortDragCoordinator(_dragService, _datastore);
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        _diagramService.Dispose();
        await _datastore.DisposeAsync();
    }

    [Fact]
    public void Attach_ConfiguresDragAndDropFlags()
    {
        // Act
        _coordinator.Attach(_builder);

        // Assert
        Assert.True(_builder.DragAndDrop.EnableInbound);
        Assert.True(_builder.DragAndDrop.EnableOutbound);
        Assert.False(_builder.DragAndDrop.DisplayElementShadow);
    }

    [Fact]
    public void Detach_DoesNotThrow()
    {
        // Arrange
        _coordinator.Attach(_builder);

        // Act & Assert
        _coordinator.Detach(_builder);
    }

    [Fact]
    public void OnDragStarted_WithDataPortChildNode_StartsDraggingWithDraggableItems()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { childNode });

        // Assert
        Assert.Contains(childNode, _dragService.DraggedItems);
    }

    [Fact]
    public void OnDragStarted_WithNonDataPortNode_StartsDraggingWithoutItems()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var node = Substitute.For<ITreeNode>();

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { node });

        // Assert
        Assert.Empty(_dragService.DraggedItems);
    }

    [Fact]
    public void OnDragStarted_WhenAllNodesLocked_StillStartsDraggingAndReplacesPreviousItems()
    {
        // Arrange - state of a previous drag with items and targets
        _coordinator.Attach(_builder);
        var previousNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        _dragService.StartDragging([previousNode], [Substitute.For<IDragTarget>()], false);
        Assert.NotEmpty(_dragService.DraggedItems);
        Assert.NotEmpty(_dragService.DragTargets!);

        var lockedNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        lockedNode.IsEditModeActive = true;

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { lockedNode });

        // Assert
        Assert.Empty(_dragService.DraggedItems);
        Assert.NotNull(_dragService.DragTargets);
        Assert.Empty(_dragService.DragTargets);
    }

    [Fact]
    public void OnDragStarted_WithDescendantOfNodeInEditMode_DoesNotAddNodeToDraggedItems()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var parentNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        parentNode.IsEditModeActive = true;
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parentNode);

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { childNode });

        // Assert
        Assert.Empty(_dragService.DraggedItems);
    }

    [Fact]
    public void OnDragStarted_WithLockedAndUnlockedNodes_OnlyDragsUnlockedNodes()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var lockedNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        lockedNode.IsEditModeActive = true;
        var unlockedNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { lockedNode, unlockedNode });

        // Assert
        Assert.Same(unlockedNode, Assert.Single(_dragService.DraggedItems));
    }

    [Fact]
    public void OnDragStarted_WithNodeInEditMode_DoesNotAddNodeToDraggedItems()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        childNode.IsEditModeActive = true;

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { childNode });

        // Assert
        Assert.Empty(_dragService.DraggedItems);
    }

    [Fact]
    public void OnDragEnded_EndsDraggingWithoutThrowing()
    {
        // Arrange
        _coordinator.Attach(_builder);

        // Act & Assert (should not throw)
        _builder.DragAndDrop.DragEnded += Raise.Event<Action>();
        Assert.Empty(_dragService.DraggedItems);
    }

    [Fact]
    public void OnDragged_ForwardsPointerMoveWithoutThrowing()
    {
        // Arrange
        _coordinator.Attach(_builder);

        // Act & Assert (should not throw)
        _builder.DragAndDrop.Dragged += Raise.Event<Action<Microsoft.AspNetCore.Components.Web.DragEventArgs>>(
            new Microsoft.AspNetCore.Components.Web.DragEventArgs());
    }

    [Fact]
    public void OnDragStarted_WithCachedDataPortTreeNode_GathersValidTargetConnectors()
    {
        // Arrange
        _coordinator.Attach(_builder);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        var dataPortTreeNode = new DataPortTreeNode { Id = childNode.Id.Value };

        _datastore.DataflowDiagramMapping.Returns(new DataflowDiagramMapping());
        _datastore.Builder.Cache.DataPortTreeNodeIds.Returns(
            new Dictionary<Guid, DataPortTreeNode> { [childNode.Id.Value] = dataPortTreeNode });

        // Act
        _builder.DragAndDrop.DragStarted += Raise.Event<Action<IEnumerable<ITreeNode>>>(new List<ITreeNode> { childNode });

        // Assert
        Assert.Contains(childNode, _dragService.DraggedItems);
    }
}
