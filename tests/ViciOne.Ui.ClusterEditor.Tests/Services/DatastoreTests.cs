using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using ViciOne.Ui.Shared.Dx.Services;
using Xunit;
using TestContext = Bunit.TestContext;

namespace ViciOne.Ui.ClusterEditor.Tests.Services;

public sealed class DatastoreTests : IAsyncDisposable
{
    private readonly ClusterBuilder _builder;
    private readonly TestContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly Guid _fbDesignId;
    private readonly SelectionManager _selectionManager;

    public DatastoreTests()
    {
        _ctx = new TestContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup services
        _ctx.Services.AddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        _ctx.Services.AddScoped<DiagramEventService>();
        _ctx.Services.AddScoped<ClusterBuilderEventBuffer>();
        _ctx.Services.AddScoped<IDatastore, Datastore>();
        _ctx.Services.AddScoped<DiagramService>();
        _ctx.Services.AddScoped<SelectionManager>();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _selectionManager = _ctx.Services.GetRequiredService<SelectionManager>();
        _diagramService.Diagram = new BlazorDiagram();

        // Create a function block design for testing
        var dependencyResolver = Substitute.For<IDependencyResolver>();
        var fbDesign = TestResources.LoadFbDesign();
        _fbDesignId = fbDesign.Id;
        dependencyResolver.ResolveFunctionBlockDesign(_fbDesignId).Returns(fbDesign);
        var clusterDependency = new ClusterDependency
        {
            Name = "TestDependency",
            Version = "1.0.0"
        };
        dependencyResolver.ResolveFunctionBlockDesignDependency(_fbDesignId).Returns(clusterDependency);

        _builder = new ClusterBuilder(dependencyResolver);
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        _ctx.Dispose();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
        _selectionManager.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_CancelsLoadContainerOperation()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);
        var container = _builder.Cluster.Dataflows[0].Root;

        // Act
        // Start LoadContainer and dispose immediately
        var loadTask = _datastore.LoadContainer(container, _diagramService, force: true);
        await _datastore.DisposeAsync();
        await loadTask;

        // Assert
        Assert.True(loadTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task DissolveContainerAsync_ExtractsAllChildrenAndSelectsThem()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        // Add multiple children
        var fbNode1 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(10, 10), Xunit.TestContext.Current.CancellationToken);
        var fbNode2 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(20, 20), Xunit.TestContext.Current.CancellationToken);
        var innerContainerNode = await _datastore.AddContainerAsync(_diagramService, new Point(30, 30), Xunit.TestContext.Current.CancellationToken);
        var labelNode = _datastore.AddLabel(new Point(40, 40), 0);

        var fb1 = _datastore.DataflowDiagramMapping.GetModel(fbNode1);
        var fb2 = _datastore.DataflowDiagramMapping.GetModel(fbNode2);
        var innerContainer = _datastore.DataflowDiagramMapping.GetModel(innerContainerNode);
        var label = _datastore.DataflowDiagramMapping.GetModel(labelNode);

        // Move all to container
        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken, [fb1, fb2, innerContainer, label]);
        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);

        _datastore.DataflowDiagramMapping.Remove(fb1);
        _datastore.DataflowDiagramMapping.Remove(fb2);
        _datastore.DataflowDiagramMapping.Remove(innerContainer);
        _datastore.DataflowDiagramMapping.Remove(label);

        // Act
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);

        // Assert
        Assert.DoesNotContain(containerNode, _diagramService.Diagram.Nodes);
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(fb1, out var newFb1Node));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(fb2, out var newFb2Node));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(innerContainer, out var newInnerContainerNode));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(label, out var newLabelNode));
        Assert.Equal(_datastore.ActiveContainer, fb1.Container);
        Assert.Equal(_datastore.ActiveContainer, fb2.Container);
        Assert.Equal(_datastore.ActiveContainer, innerContainer.Parent);
        Assert.Equal(_datastore.ActiveContainer, label.Container);

        Assert.True(_selectionManager.IsSelected(newFb1Node));
        Assert.True(_selectionManager.IsSelected(newFb2Node));
        Assert.True(_selectionManager.IsSelected(newInnerContainerNode));
        Assert.True(_selectionManager.IsSelected(newLabelNode));
    }

    [Fact]
    public async Task DissolveContainerAsync_ManagesAffectedLinksCorrectly()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);
        var outerFbNode = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(50, 50), Xunit.TestContext.Current.CancellationToken);
        var innerFbNode = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(150, 50), Xunit.TestContext.Current.CancellationToken);
        var nestedFbNode = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(250, 50), Xunit.TestContext.Current.CancellationToken);
        var outerFb = _datastore.DataflowDiagramMapping.GetModel(outerFbNode);
        var innerFb = _datastore.DataflowDiagramMapping.GetModel(innerFbNode);
        var nestedFb = _datastore.DataflowDiagramMapping.GetModel(nestedFbNode);
        var outerFbOutput = outerFb.SystemOutputs.First();
        var innerFbInput = innerFb.SystemInputs.First();
        var nestedFbInput = nestedFb.SystemInputs.First();

        var outerInnerLink = _builder.Editors.Connector.AddLink(outerFbOutput, innerFbInput);
        var outerInnerLinkNode = LinkMapper.CreateLink(_datastore, outerInnerLink);
        _datastore.DataflowDiagramMapping.Add(outerInnerLink, outerInnerLinkNode);

        var outerNestedLink = _builder.Editors.Connector.AddLink(outerFbOutput, nestedFbInput);
        var outerNestedLinkNode = LinkMapper.CreateLink(_datastore, outerNestedLink);
        _datastore.DataflowDiagramMapping.Add(outerNestedLink, outerNestedLinkNode);

        var nestedContainerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken, [nestedFb]);
        var nestedContainer = _datastore.DataflowDiagramMapping.GetModel(nestedContainerNode);

        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken, [innerFb, nestedContainer]);

        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);
        _datastore.DataflowDiagramMapping.Remove(innerFb);
        _datastore.DataflowDiagramMapping.Remove(nestedFb);
        _datastore.DataflowDiagramMapping.Remove(nestedContainer);

        // Act
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);

        // Assert
        Assert.DoesNotContain(containerNode, _diagramService.Diagram.Nodes);

        Assert.True(_datastore.DataflowDiagramMapping.ContainsMapping(outerInnerLink));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(outerInnerLink, out var newLinkNode));
        Assert.NotEqual(outerInnerLinkNode, newLinkNode);
        Assert.Equal(newLinkNode.SourceNode, outerFbNode);
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(innerFb, out var newInnerFbNode));
        Assert.Equal(newLinkNode.TargetNode, newInnerFbNode);

        Assert.True(_datastore.DataflowDiagramMapping.ContainsMapping(outerNestedLink));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(outerInnerLink, out var newNestedLinkNode));
        Assert.NotEqual(outerNestedLinkNode, newNestedLinkNode);
        Assert.Equal(newNestedLinkNode.SourceNode, outerFbNode);
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(nestedContainer, out var newNestedContainerNode));
        Assert.Equal(1, newNestedContainerNode.ConnectorsToList().Count(c => c.HasLink()));

        outerInnerLinkNode.Dispose();
        outerNestedLinkNode.Dispose();
    }

    [Fact]
    public async Task DissolveContainerAsync_PositionsChildrenRelativeToContainerPosition()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var fbNode1 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(0, 0), Xunit.TestContext.Current.CancellationToken);
        var fbNode2 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(180, 0), Xunit.TestContext.Current.CancellationToken);
        var fbNode3 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(0, 200), Xunit.TestContext.Current.CancellationToken);
        var fbNode4 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(180, 200), Xunit.TestContext.Current.CancellationToken);
        var functionBlock1 = _datastore.DataflowDiagramMapping.GetModel(fbNode1);
        var functionBlock2 = _datastore.DataflowDiagramMapping.GetModel(fbNode2);
        var functionBlock3 = _datastore.DataflowDiagramMapping.GetModel(fbNode3);
        var functionBlock4 = _datastore.DataflowDiagramMapping.GetModel(fbNode4);

        var containerPosition = new Point(200, 200);
        var containerNode = await _datastore.AddContainerAsync(_diagramService, containerPosition, Xunit.TestContext.Current.CancellationToken, [functionBlock1, functionBlock2, functionBlock3, functionBlock4]);
        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);

        var containerPoint = new Point(
            (childContainer.X ?? 0) + (BlockNodeLayout.Width / 2.0),
            (childContainer.Y ?? 0) + (BlockNodeLayout.RowHeight * (BlockNodeLayout.SystemConnectorRows + BlockNodeLayout.MinimumConnectorRows) / 2)
        );

        var fbList = new List<NodeModel>() { fbNode1, fbNode2, fbNode3, fbNode4 };
        var childBoundCenter = fbList.GetBounds().Center;
        var deltaX = containerPoint.X - childBoundCenter.X;
        var deltaY = containerPoint.Y - childBoundCenter.Y;

        _datastore.DataflowDiagramMapping.Remove(functionBlock1);
        _datastore.DataflowDiagramMapping.Remove(functionBlock2);
        _datastore.DataflowDiagramMapping.Remove(functionBlock3);
        _datastore.DataflowDiagramMapping.Remove(functionBlock4);

        // Act
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);

        // Assert
        var extractedFbNode1 = _datastore.DataflowDiagramMapping.GetDiagramModel(functionBlock1);
        Assert.NotNull(extractedFbNode1);
        Assert.Equal(fbNode1.Position.X + deltaX, extractedFbNode1.Position.X);
        Assert.Equal(fbNode1.Position.Y + deltaY, extractedFbNode1.Position.Y);

        var extractedFbNode2 = _datastore.DataflowDiagramMapping.GetDiagramModel(functionBlock2);
        Assert.NotNull(extractedFbNode2);
        Assert.Equal(fbNode2.Position.X + deltaX, extractedFbNode2.Position.X);
        Assert.Equal(fbNode2.Position.Y + deltaY, extractedFbNode2.Position.Y);

        var extractedFbNode3 = _datastore.DataflowDiagramMapping.GetDiagramModel(functionBlock3);
        Assert.NotNull(extractedFbNode3);
        Assert.Equal(fbNode3.Position.X + deltaX, extractedFbNode3.Position.X);
        Assert.Equal(fbNode3.Position.Y + deltaY, extractedFbNode3.Position.Y);

        var extractedFbNode4 = _datastore.DataflowDiagramMapping.GetDiagramModel(functionBlock4);
        Assert.NotNull(extractedFbNode4);
        Assert.Equal(fbNode4.Position.X + deltaX, extractedFbNode4.Position.X);
        Assert.Equal(fbNode4.Position.Y + deltaY, extractedFbNode4.Position.Y);
    }

    [Fact]
    public async Task DissolveContainerAsync_RemovesEmptyContainerFromDiagram()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);
        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken);
        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);

        // Act
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);

        // Assert
        Assert.DoesNotContain(containerNode, _diagramService.Diagram.Nodes);
        Assert.False(_datastore.DataflowDiagramMapping.TryGetDiagramModel(childContainer, out _));
    }

    [Fact]
    public async Task DissolveContainerAsync_ReturnsGracefullyIfContainerDiagramModelIsNotFound()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(200, 200), Xunit.TestContext.Current.CancellationToken, []);
        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);
        _datastore.DataflowDiagramMapping.Remove(childContainer);

        // Act + Assert - Should not throw any exception
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);
    }

    [Fact]
    public async Task DissolveContainerAsync_ReturnsGracefullyIfDisposeWasCalledBefore()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(200, 200), Xunit.TestContext.Current.CancellationToken, []);
        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);
        _datastore.DataflowDiagramMapping.Remove(childContainer);

        await _datastore.DisposeAsync();

        // Act + Assert - Should not throw any exception
        await _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager);
    }

    [Fact]
    public async Task DissolveContainerAsync_WithDifferentContainers_ExecutesAllInParallel()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var fbNode1 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(50, 50), Xunit.TestContext.Current.CancellationToken);
        var fb1 = _datastore.DataflowDiagramMapping.GetModel(fbNode1);
        var containerNode1 = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken, [fb1]);
        var childContainer1 = _datastore.DataflowDiagramMapping.GetModel(containerNode1);
        _datastore.DataflowDiagramMapping.Remove(fb1);

        var fbNode2 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(150, 150), Xunit.TestContext.Current.CancellationToken);
        var fb2 = _datastore.DataflowDiagramMapping.GetModel(fbNode2);
        var containerNode2 = await _datastore.AddContainerAsync(_diagramService, new Point(200, 200), Xunit.TestContext.Current.CancellationToken, [fb2]);
        var childContainer2 = _datastore.DataflowDiagramMapping.GetModel(containerNode2);
        _datastore.DataflowDiagramMapping.Remove(fb2);

        var fbNode3 = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(250, 250), Xunit.TestContext.Current.CancellationToken);
        var fb3 = _datastore.DataflowDiagramMapping.GetModel(fbNode3);
        var containerNode3 = await _datastore.AddContainerAsync(_diagramService, new Point(300, 300), Xunit.TestContext.Current.CancellationToken, [fb3]);
        var childContainer3 = _datastore.DataflowDiagramMapping.GetModel(containerNode3);
        _datastore.DataflowDiagramMapping.Remove(fb3);

        // Act
        var task1 = _datastore.DissolveContainerAsync(childContainer1, _diagramService, _selectionManager);
        var task2 = _datastore.DissolveContainerAsync(childContainer2, _diagramService, _selectionManager);
        var task3 = _datastore.DissolveContainerAsync(childContainer3, _diagramService, _selectionManager);

        await Task.WhenAll(task1, task2, task3);

        // Assert
        Assert.True(task1.IsCompletedSuccessfully);
        Assert.True(task2.IsCompletedSuccessfully);
        Assert.True(task3.IsCompletedSuccessfully);

        Assert.DoesNotContain(containerNode1, _diagramService.Diagram.Nodes);
        Assert.DoesNotContain(containerNode2, _diagramService.Diagram.Nodes);
        Assert.DoesNotContain(containerNode3, _diagramService.Diagram.Nodes);

        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(fb1, out _));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(fb2, out _));
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(fb3, out _));

        Assert.Equal(_datastore.ActiveContainer, fb1.Container);
        Assert.Equal(_datastore.ActiveContainer, fb2.Container);
        Assert.Equal(_datastore.ActiveContainer, fb3.Container);
    }

    [Fact]
    public async Task DissolveContainerAsync_WithDisposeAsyncDuringExecution_CancelsAllOperations()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var childContainers = new List<ChildContainer>();

        foreach (var i in Enumerable.Range(1, 1000))
        {
            var fbNode = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(50, i), Xunit.TestContext.Current.CancellationToken);
            var fb = _datastore.DataflowDiagramMapping.GetModel(fbNode);
            var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, i + 100), Xunit.TestContext.Current.CancellationToken, [fb]);
            var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);
            childContainers.Add(childContainer);
            _datastore.DataflowDiagramMapping.Remove(fb);
        }

        // Act 
        var tasks = childContainers.Select(
            childContainer => _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager));

        await _datastore.DisposeAsync();

        await Task.WhenAll(tasks);

        // Assert
        foreach (var task in tasks)
            Assert.True(task.IsCompletedSuccessfully || task.IsCanceled);
    }

    [Fact]
    public async Task DissolveContainerAsync_WithSameContainerMultipleTimes_ExecutesOnlyOnce()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        var fbNode = await _datastore.AddFunctionBlockAsync(_diagramService, _fbDesignId, new Point(50, 50), Xunit.TestContext.Current.CancellationToken);
        var functionBlock = _datastore.DataflowDiagramMapping.GetModel(fbNode);

        var containerNode = await _datastore.AddContainerAsync(_diagramService, new Point(100, 100), Xunit.TestContext.Current.CancellationToken, [functionBlock]);

        var childContainer = _datastore.DataflowDiagramMapping.GetModel(containerNode);
        _datastore.DataflowDiagramMapping.Remove(functionBlock);

        // Act - Start multiple dissolve operations with the same container
        var tasks = Enumerable.Range(1, 1000)
            .Select(_ => _datastore.DissolveContainerAsync(childContainer, _diagramService, _selectionManager));

        await Task.WhenAll(tasks);

        // Assert - All tasks should complete successfully
        foreach (var task in tasks)
            Assert.True(task.IsCompletedSuccessfully);

        // The container should be dissolved (removed from diagram)
        Assert.DoesNotContain(containerNode, _diagramService.Diagram.Nodes);

        // The function block should be extracted to active container
        Assert.True(_datastore.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _));
        Assert.Equal(_datastore.ActiveContainer, functionBlock.Container);

        // Verify that the selection manager has the extracted node selected
        var extractedFbNode = _datastore.DataflowDiagramMapping.GetDiagramModel(functionBlock);
        Assert.True(_selectionManager.IsSelected(extractedFbNode));
    }

    [Fact]
    public async Task LoadContainer_AfterDisposeAsync_DoesNotThrowObjectDisposedException()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);

        // Act
        // Don't await the dispose task to simulate race condition
        var disposeTask = _datastore.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _datastore.LoadContainer(_builder.Cluster.Dataflows[0].Root, _diagramService);
        });
        // Ensure dispose completes
        await disposeTask;

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task LoadContainer_CalledMultipleTimes_CancelsAndReplacesToken()
    {
        // Arrange
        await _datastore.Load(_builder, _diagramService);
        var container = _builder.Cluster.Dataflows[0].Root;

        // Act
        var tasks = Enumerable.Range(1, 1000)
            .Select(_ => _datastore.LoadContainer(container, _diagramService, force: true));

        await Task.WhenAll(tasks);

        // Assert
        foreach (var task in tasks)
            Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task LoadContainer_WithNullBuilder_DoesNotCrash()
    {
        // Arrange
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Name = "TestContainer"
        };

        // Assert
        // Should throw InvalidOperationException from Builder property
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _datastore.LoadContainer(container, _diagramService);
        });
    }
}
