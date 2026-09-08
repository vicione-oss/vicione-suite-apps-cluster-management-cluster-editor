using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Owns loading a cluster builder and switching the active container, including the
/// viewport persistence and the internal cancellation coordination that prevents
/// overlapping <see cref="LoadContainer"/> calls from corrupting the diagram.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed partial class ContainerLoadService(
    BuilderEventProjectionService builderEvents,
    ClusterBuilderEventBuffer clusterBuilderEventBuffer,
    DatastoreState state,
    DiagramEventService diagramEventService,
    DiagramProjectionService projection,
    ILogger<ContainerLoadService> logger) : IAsyncDisposable
{
    private bool _disposed;
    private CancellationTokenSource? _loadContainerCts;
    private readonly SemaphoreSlim _loadContainerCtsSemaphore = new(1);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        await _loadContainerCtsSemaphore.WaitAsync();
        try
        {
            if (_loadContainerCts is not null)
            {
                await _loadContainerCts.CancelAsync();
                _loadContainerCts.Dispose();
            }
        }
        finally
        {
            _loadContainerCtsSemaphore.Release();
        }

        _loadContainerCtsSemaphore.Dispose();
    }

    public async Task Load(IClusterBuilder builder, DiagramService diagramService, CancellationToken cancellationToken)
    {
        // Fast fail if cancellation has already been requested
        cancellationToken.ThrowIfCancellationRequested();

        state.SetBuilder(builder);
        clusterBuilderEventBuffer.SetBuilder(builder);

        await state.InvokeBuilderChanged();

        var currentDataflow = state.Builder.Cluster.Dataflows[0];

        await LoadContainer(currentDataflow.Root, diagramService, cancellationToken);
    }

    public async Task LoadContainer(Container container, DiagramService diagramService, CancellationToken? externalCancellationToken = null, bool force = false)
    {
        if (_disposed)
            return;

        if (externalCancellationToken is null)
        {
            // If there is no external cancellation token, we need to use the _loadContainerCts to cancel previous internal LoadContainer calls
            // if a new one is made before the previous one finishes. We have to do it internally here to coordinate all occurences where LoadContainer
            // is called, since the different callers don't know each other.
            try
            {
                await _loadContainerCtsSemaphore.WaitAsync();
                try
                {
                    // Cancel previous call to LoadContainer if there was one
                    if (_loadContainerCts is not null)
                    {
                        await _loadContainerCts.CancelAsync();
                        _loadContainerCts.Dispose();
                    }

                    _loadContainerCts = new CancellationTokenSource();
                    await LoadContainerSafely(container, diagramService, _loadContainerCts.Token, force);
                    _loadContainerCts = null;

                }
                finally
                {
                    _loadContainerCtsSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Nothing to do here, return gracefully
            }
            catch (ObjectDisposedException) when (_disposed)
            {
                // Semaphore or other object already disposed, nothing we can do, return gracefully
            }
        }
        else
        {
            // If there is an external cancellation token, we assume that the caller is responsible for cancelling previous calls
            // to LoadContainer. We just cancel the last previous internal call if there is one. We also don't handle any Exceptions here because
            // the caller need to know about them and must handle them, since they are responsible for the cancellation in this case.
            await _loadContainerCtsSemaphore.WaitAsync();
            try
            {
                // Cancel previous internal call to LoadContainer if there was one
                if (_loadContainerCts is not null)
                {
                    await _loadContainerCts.CancelAsync();
                    _loadContainerCts.Dispose();
                }
                _loadContainerCts = null;

                await LoadContainerSafely(container, diagramService, externalCancellationToken.Value, force);
            }
            finally
            {
                _loadContainerCtsSemaphore.Release();
            }
        }
    }

    [LoggerMessage(1, LogLevel.Error, "Failed to load container {ContainerName}. {Message}", EventName = "LoadContainerFailed")]
    private static partial void LoadContainerFailed(ILogger logger, string containerName, string message);

    private async Task LoadContainerSafely(Container container, DiagramService diagramService, CancellationToken cancellationToken, bool force = false)
    {
        // Fast fail if cancellation has already been requested
        cancellationToken.ThrowIfCancellationRequested();

        if (state.ActiveContainer == container && !force)
            return;

        SaveViewport(state.ActiveContainer, diagramService.Diagram);

        state.SetActiveContainer(container);
        var activeDataflow = container is ChildContainer childContainer
            ? state.Builder.Cache.GetDataflow(childContainer)
            : state.Builder.Cache.Dataflows.FirstOrDefault(x => x.Root == container);

        if (activeDataflow is null)
        {
            LoadContainerFailed(logger, container.Name, "Dataflow not found in builder cache.");
            return;
        }

        state.SetActiveDataflow(activeDataflow);

        state.SetValidDataflowEngines(state.Builder.Cache.GetUsedEngines(state.ActiveDataflow).Concat(state.Builder.Cache.GetUnusedEngines()));

        SearchBlocksEventService.RequestResetFindResult();

        builderEvents.Detach();
        diagramService.DiagramState.SuppressEvents = true;

        diagramService.Diagram.UnselectAll();
        diagramService.Diagram.Nodes.Clear();
        state.DataflowDiagramMapping.Clear();

        if (container.ViewportX.HasValue && container.ViewportY.HasValue && container.Zoom.HasValue)
        {
            diagramService.Diagram.SetPan(container.ViewportX.Value, container.ViewportY.Value);
            diagramService.Diagram.SetZoom(container.Zoom.Value);
        }

        await projection.AddFunctionBlocksToMapping(container.FunctionBlocks, diagramService, cancellationToken);
        await projection.AddChildContainersToMapping(container.Containers, diagramService, cancellationToken);
        projection.AddLabelsToMapping(container.Labels);

        var links = new List<Link>();
        foreach (var link in projection.GetActiveContainerFunctionBlockLinks())
            links.Add(link);
        foreach (var link in projection.GetActiveContainerContainerLinks())
            links.Add(link);
        projection.AddLinksToMapping(links);

        ModelDiagramMapper.AddToDiagram(diagramService.Diagram, state.DataflowDiagramMapping.GetNodes(), state.DataflowDiagramMapping.GetNodeLinks());

        builderEvents.Attach();
        diagramService.DiagramState.SuppressEvents = false;

        await diagramEventService.InvokeContainerLoaded(state.ActiveContainer);

        if (force)
            await state.InvokeForcedRefreshRequested();
    }

    public void SaveViewport(DiagramService diagramService)
        => SaveViewport(state.ActiveContainer, diagramService.Diagram);

    private static void SaveViewport(Container container, global::Blazor.Diagrams.Core.Diagram diagram)
    {
        container.ViewportX = diagram.Pan.X;
        container.ViewportY = diagram.Pan.Y;
        container.Zoom = diagram.Zoom;
    }
}
