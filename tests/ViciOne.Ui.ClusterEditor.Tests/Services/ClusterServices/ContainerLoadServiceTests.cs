using System;
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
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Services.ClusterServices;

public sealed class ContainerLoadServiceTests : IAsyncDisposable
{
    private readonly IClusterBuilder _builder;
    private readonly BunitContext _ctx;
    private readonly IDatastore _datastore;
    private readonly DiagramEventService _diagramEvents;
    private readonly DiagramService _diagramService;
    private readonly ClusterEditService _editService;
    private readonly Guid _fbDesignId;
    private readonly ILogger<ContainerLoadService> _logger;
    private readonly DatastoreState _state;
    private readonly ContainerLoadService _sut;

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    private Container Root => _builder.Cluster.Dataflows[0].Root;

    public ContainerLoadServiceTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop
            .Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true)
            .SetResult([.. Enumerable.Repeat(0, 10)]);

        _logger = Substitute.For<ILogger<ContainerLoadService>>();

        _ctx.Services.AddSingleton(new ComparerService([], Substitute.For<ILogger<ComparerService>>()));
        _ctx.Services.AddSingleton(_logger);
        _ctx.Services.AddScoped<DiagramEventService>();
        _ctx.Services.AddScoped<ClusterBuilderEventBuffer>();
        _ctx.Services.AddDatastore();
        _ctx.Services.AddScoped<DiagramService>();

        _datastore = _ctx.Services.GetRequiredService<IDatastore>();
        _diagramService = _ctx.Services.GetRequiredService<DiagramService>();
        _diagramService.Diagram = new BlazorDiagram();
        _state = _ctx.Services.GetRequiredService<DatastoreState>();
        _diagramEvents = _ctx.Services.GetRequiredService<DiagramEventService>();
        _editService = _ctx.Services.GetRequiredService<ClusterEditService>();
        _sut = _ctx.Services.GetRequiredService<ContainerLoadService>();

        _builder = BuilderFactory.Create();
        _fbDesignId = BuilderFactory.FbDesignId;
    }

    public async ValueTask DisposeAsync()
    {
        await _sut.DisposeAsync();
        _builder.Dispose();
        await _ctx.DisposeAsync();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
    }

    private Task LoadRootAsync() => _sut.Load(_builder, _diagramService, CancellationToken.None);

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
    public async Task DisposeAsync_WhileLoadContainerPending_CompletesGracefully()
    {
        // Arrange
        await LoadRootAsync();

        // Act - start an internal load and dispose without awaiting it first
        var loadTask = _sut.LoadContainer(Root, _diagramService, force: true);
        await _sut.DisposeAsync();
        await loadTask;

        // Assert
        loadTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    // --- Load -------------------------------------------------------------------------------------

    [Fact]
    public async Task Load_WhenTokenAlreadyCanceled_ThrowsAndLeavesBuilderUnset()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await _sut.Load(_builder, _diagramService, cts.Token);

        // Assert - fast-fail happens before the builder is assigned
        await act.Should().ThrowAsync<OperationCanceledException>();
        _state.HasBuilder.Should().BeFalse();
    }

    [Fact]
    public async Task Load_WithValidBuilder_SetsBuilderRaisesBuilderChangedAndLoadsRootContainer()
    {
        // Arrange
        var builderChanged = false;
        _state.BuilderChanged += () => { builderChanged = true; return Task.CompletedTask; };
        Container? loadedContainer = null;
        _diagramEvents.ContainerLoaded += c =>
        {
            loadedContainer = c;
            return Task.CompletedTask;
        };

        // Act
        await LoadRootAsync();

        // Assert
        _state.HasBuilder.Should().BeTrue();
        builderChanged.Should().BeTrue();
        _state.ActiveContainer.Should().BeSameAs(Root);
        _state.ActiveDataflow.Should().BeSameAs(_builder.Cluster.Dataflows[0]);
        loadedContainer.Should().BeSameAs(Root);
    }

    // --- LoadContainer: coordination, cancellation & disposal -------------------------------------

    [Fact]
    public async Task LoadContainer_AfterDisposeAsync_ReturnsWithoutRaisingContainerLoaded()
    {
        // Arrange
        await LoadRootAsync();
        var raised = false;
        _diagramEvents.ContainerLoaded += _ =>
        {
            raised = true;
            return Task.CompletedTask;
        };
        await _sut.DisposeAsync();

        // Act
        var act = async () => await _sut.LoadContainer(Root, _diagramService, force: true);

        // Assert - the disposed guard short-circuits the call gracefully
        await act.Should().NotThrowAsync();
        raised.Should().BeFalse();
    }

    [Fact]
    public async Task LoadContainer_CalledConcurrently_AllTasksCompleteSuccessfully()
    {
        // Arrange
        await LoadRootAsync();

        // Act - overlapping internal calls must cancel/replace the previous token and swallow the
        // resulting OperationCanceledException without surfacing it to any caller
        var tasks = Enumerable
            .Range(0, 200)
            .Select(_ => _sut.LoadContainer(Root, _diagramService, force: true))
            .ToArray();
        await Task.WhenAll(tasks);

        // Assert
        tasks.Should().OnlyContain(t => t.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task LoadContainer_WithCanceledExternalToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await LoadRootAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = async () => await _sut.LoadContainer(Root, _diagramService, cts.Token, force: true);

        // Assert - the external-token path leaves cancellation handling to the caller
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // --- LoadContainer: switching, projection, viewport & events ----------------------------------

    [Fact]
    public async Task LoadContainer_IntoChildContainer_SetsActiveContainerToChild()
    {
        // Arrange
        await LoadRootAsync();
        var childNode = await _editService.AddChildContainer(_diagramService, new Point(50, 50), Ct);
        var childContainer = _state.DataflowDiagramMapping.GetModel(childNode);

        // Act
        await _sut.LoadContainer(childContainer, _diagramService);

        // Assert - the ChildContainer branch resolves the owning dataflow via the builder cache
        _state.ActiveContainer.Should().BeSameAs(childContainer);
    }

    [Fact]
    public async Task LoadContainer_RequestsSearchResultReset()
    {
        // Arrange
        await LoadRootAsync();
        var resetRequested = false;
        void Handler() => resetRequested = true;
        SearchBlocksEventService.ResetFindResultRequested += Handler;

        // Act
        try
        {
            await _sut.LoadContainer(Root, _diagramService, force: true);
        }
        finally
        {
            SearchBlocksEventService.ResetFindResultRequested -= Handler;
        }

        // Assert
        resetRequested.Should().BeTrue();
    }

    [Fact]
    public async Task LoadContainer_SwitchingContainers_ClearsPreviousNodesThenProjectsTargetContent()
    {
        // Arrange
        await LoadRootAsync();
        var fbNode = await _editService.AddFunctionBlock(_diagramService, _fbDesignId, new Point(10, 10), Ct);
        var functionBlock = _state.DataflowDiagramMapping.GetModel(fbNode);
        _editService.AddDataflow();
        var emptyRoot = _builder.Cluster.Dataflows[1].Root;

        // Act - a forced reload re-projects the root's content from the model...
        await _sut.LoadContainer(Root, _diagramService, force: true);
        var nodesAfterReload = _diagramService.Diagram.Nodes.Count;
        var mappedAfterReload = _state.DataflowDiagramMapping.TryGetDiagramModel(functionBlock, out _);

        // ...and switching to an empty container clears the previously projected nodes
        await _sut.LoadContainer(emptyRoot, _diagramService);

        // Assert
        nodesAfterReload.Should().Be(1);
        mappedAfterReload.Should().BeTrue();
        _diagramService.Diagram.Nodes.Should().BeEmpty();
        _diagramService.DiagramState.SuppressEvents.Should().BeFalse();
    }

    [Fact]
    public async Task LoadContainer_SwitchingContainers_SavesPreviousViewportAndRestoresTargetViewport()
    {
        // Arrange
        await LoadRootAsync();
        _editService.AddDataflow();
        var target = _builder.Cluster.Dataflows[1].Root;
        target.ViewportX = 123;
        target.ViewportY = 234;
        target.Zoom = 1.75;

        _diagramService.Diagram.SetPan(10, 20);
        _diagramService.Diagram.SetZoom(0.5);

        // Act
        await _sut.LoadContainer(target, _diagramService);

        // Assert - the previously active container persists the current viewport...
        Root.ViewportX.Should().Be(10d);
        Root.ViewportY.Should().Be(20d);
        Root.Zoom.Should().Be(0.5d);

        // ...and the target container's saved viewport is restored onto the diagram
        _diagramService.Diagram.Pan.X.Should().Be(123d);
        _diagramService.Diagram.Pan.Y.Should().Be(234d);
        _diagramService.Diagram.Zoom.Should().Be(1.75d);
    }

    [Fact]
    public async Task LoadContainer_SwitchingToContainerInAnotherDataflow_UpdatesActiveDataflow()
    {
        // Arrange
        await LoadRootAsync();
        _editService.AddDataflow();
        var secondDataflow = _builder.Cluster.Dataflows[1];
        var dataflowChanged = false;
        _state.ActiveDataflowChanged += () => dataflowChanged = true;

        // Act
        await _sut.LoadContainer(secondDataflow.Root, _diagramService);

        // Assert
        _state.ActiveDataflow.Should().BeSameAs(secondDataflow);
        _state.ActiveContainer.Should().BeSameAs(secondDataflow.Root);
        dataflowChanged.Should().BeTrue();
    }

    [Fact]
    public async Task LoadContainer_WhenTargetContainerHasNoDataflowInCache_AbortsAndLogsError()
    {
        // Arrange
        await LoadRootAsync();
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        var raised = false;
        _diagramEvents.ContainerLoaded += _ =>
        {
            raised = true;
            return Task.CompletedTask;
        };
        var orphan = new Container { Id = Guid.NewGuid(), Name = "Orphan" };

        // Act - the orphan belongs to no dataflow, so resolution fails after the active container is set
        var act = async () => await _sut.LoadContainer(orphan, _diagramService);

        // Assert
        await act.Should().NotThrowAsync();
        raised.Should().BeFalse();
        _logger
            .ReceivedCalls()
            .Any(call => call.GetMethodInfo().Name == nameof(ILogger.Log)
                         && call.GetArguments() is [LogLevel.Error, ..])
            .Should().BeTrue();
    }

    [Fact]
    public async Task LoadContainer_WithForce_RaisesForcedRefreshRequested()
    {
        // Arrange
        await LoadRootAsync();
        var forcedRefresh = false;
        _state.ForcedRefreshRequested += () => { forcedRefresh = true; return Task.CompletedTask; };

        // Act
        await _sut.LoadContainer(Root, _diagramService, force: true);

        // Assert
        forcedRefresh.Should().BeTrue();
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task LoadContainer_WithSameActiveContainer_RaisesContainerLoadedOnlyWhenForced(bool force, int expectedLoadCount)
    {
        // Arrange
        await LoadRootAsync();
        var loadCount = 0;
        _diagramEvents.ContainerLoaded += _ =>
        {
            loadCount++;
            return Task.CompletedTask;
        };

        // Act
        await _sut.LoadContainer(Root, _diagramService, force: force);

        // Assert - reloading the active container is a no-op unless forced
        loadCount.Should().Be(expectedLoadCount);
    }

    // --- SaveViewport -----------------------------------------------------------------------------

    [Fact]
    public async Task SaveViewport_PersistsCurrentDiagramPanAndZoomToActiveContainer()
    {
        // Arrange
        await LoadRootAsync();
        _diagramService.Diagram.SetPan(50, 60);
        _diagramService.Diagram.SetZoom(1.25);

        // Act
        _sut.SaveViewport(_diagramService);

        // Assert
        Root.ViewportX.Should().Be(50d);
        Root.ViewportY.Should().Be(60d);
        Root.Zoom.Should().Be(1.25d);
    }
}
