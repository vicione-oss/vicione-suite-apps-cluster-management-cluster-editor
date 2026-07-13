using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Diagrams.Core.Extensions;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Owns the structural reshaping operations that move children between containers:
/// dissolving a child container back into its parent and grouping a selection into a
/// new child container. Coordinates its own cancellation for the dissolve operation.
/// </summary>
internal sealed class ContainerRestructureService(ClusterEditService editService, DiagramProjectionService projection, DatastoreState state) : IAsyncDisposable
{
    private bool _disposed;
    private CancellationTokenSource? _dissolveContainerCts;
    private readonly SemaphoreSlim _dissolveContainerSemaphore = new(1);

    private List<BlockNodeLink> CreateMissingActiveContainerLinks()
    {
        var newLinks = new List<BlockNodeLink>();
        foreach (var link in projection.GetActiveContainerFunctionBlockLinks().Concat(projection.GetActiveContainerContainerLinks()))
        {
            if (state.DataflowDiagramMapping.ContainsMapping(link))
                continue;

            var node = LinkMapper.CreateLink(state, link);
            newLinks.Add(node);
            state.DataflowDiagramMapping.Add(link, node);
        }

        return newLinks;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        await _dissolveContainerSemaphore.WaitAsync();
        try
        {
            if (_dissolveContainerCts is not null)
            {
                await _dissolveContainerCts.CancelAsync();
                _dissolveContainerCts.Dispose();
            }
        }
        finally
        {
            _dissolveContainerSemaphore.Release();
        }

        _dissolveContainerSemaphore.Dispose();
    }

    public async Task DissolveContainerAsync(ChildContainer childContainer, DiagramService diagramService, SelectionManager selectionManager)
    {
        if (_disposed)
            return;

        try
        {
            await _dissolveContainerSemaphore.WaitAsync();
            try
            {
                if (!state.DataflowDiagramMapping.TryGetDiagramModel(childContainer, out var childContainerNode))
                    return;

                _dissolveContainerCts = new();

                var containerPoint = GetContainerCenter(childContainer);

                RemoveDissolvedContainerFromDiagram(childContainer, childContainerNode, diagramService);

                var dissolvedChilds = state.Builder.Editors.Container.DissolveContainer(childContainer);
                var newNodes = await ProjectDissolvedChildrenAsync(dissolvedChilds, diagramService, _dissolveContainerCts.Token);

                RepositionNodes(newNodes, containerPoint);

                var newLinks = CreateMissingActiveContainerLinks();

                ModelDiagramMapper.AddToDiagram(diagramService.Diagram, newNodes, newLinks);
                selectionManager.Select(newNodes.Cast<IDiagramModel>());
            }
            finally
            {
                _dissolveContainerCts?.Dispose();
                _dissolveContainerCts = null;

                _dissolveContainerSemaphore.Release();
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

    private static Point GetContainerCenter(ChildContainer childContainer)
        => new(
            (childContainer.X ?? 0) + (BlockNodeLayout.Width / 2.0),
            (childContainer.Y ?? 0) + (BlockNodeLayout.RowHeight * (BlockNodeLayout.SystemConnectorRows + BlockNodeLayout.MinimumConnectorRows) / 2)
        );

    public async Task MoveToNewContainerAsync(
        DiagramService diagramService,
        Point newContainerLocation,
        IEnumerable<FunctionBlock> functionBlocks,
        IEnumerable<ChildContainer> containers,
        IEnumerable<Label> labels,
        SelectionManager selectionManager)
    {
        var fbArray = functionBlocks.ToArray();
        var containerArray = containers.ToArray();
        var labelsArray = labels.ToArray();

        var affectedLinks = new List<Link>();
        foreach (var fb in fbArray)
        {
            foreach (var connector in fb.GetConnectors())
            {
                foreach (var link in connector.GetVisibleLinksConnectedToThis())
                    affectedLinks.Add(link);
            }
        }

        foreach (var container in containerArray)
        {
            foreach (var connector in container.GetConnectors())
            {
                foreach (var link in connector.GetVisibleLinksConnectedToThis())
                    affectedLinks.Add(link);
            }
        }

        ModelDiagramMapper.RemoveNodesFromDiagram(
            diagramService,
            state.DataflowDiagramMapping.GetDiagramModels(fbArray)
            .Cast<NodeModel>()
            .Concat(state.DataflowDiagramMapping.GetDiagramModels(containerArray))
            .Concat(state.DataflowDiagramMapping.GetDiagramModels(labelsArray))
        );

        foreach (var link in affectedLinks)
            state.DataflowDiagramMapping.Remove(link);
        foreach (var fb in fbArray)
            state.DataflowDiagramMapping.Remove(fb);
        foreach (var container in containerArray)
            state.DataflowDiagramMapping.Remove(container);
        foreach (var label in labelsArray)
            state.DataflowDiagramMapping.Remove(label);

        var containerNode = await editService.AddChildContainer(
            diagramService,
            newContainerLocation,
            default,
            [.. fbArray.Cast<IContainerChild>(), .. containerArray, .. labelsArray]
        );

        var newLinks = CreateMissingActiveContainerLinks();

        ModelDiagramMapper.AddToDiagram(diagramService.Diagram, [containerNode], newLinks);

        selectionManager.Select(containerNode);
    }

    private async Task<List<NodeModel>> ProjectDissolvedChildrenAsync(IEnumerable<IContainerChild> dissolvedChilds, DiagramService diagramService, CancellationToken cancellationToken)
    {
        var dissolvedFbs = new List<FunctionBlock>();
        var dissolvedContainers = new List<ChildContainer>();
        var dissolvedLabels = new List<Label>();

        foreach (var child in dissolvedChilds)
        {
            if (child is FunctionBlock fb)
                dissolvedFbs.Add(fb);
            else if (child is ChildContainer cc)
                dissolvedContainers.Add(cc);
            else if (child is Label label)
                dissolvedLabels.Add(label);
        }

        var newNodes = new List<NodeModel>(dissolvedFbs.Count + dissolvedContainers.Count + dissolvedLabels.Count);
        newNodes.AddRange(await projection.AddFunctionBlocksToMapping(dissolvedFbs, diagramService, cancellationToken));
        newNodes.AddRange(await projection.AddChildContainersToMapping(dissolvedContainers, diagramService, cancellationToken));
        newNodes.AddRange(projection.AddLabelsToMapping(dissolvedLabels));

        return newNodes;
    }

    private void RemoveDissolvedContainerFromDiagram(ChildContainer childContainer, ChildContainerNode childContainerNode, DiagramService diagramService)
    {
        ModelDiagramMapper.RemoveNodesFromDiagram(diagramService, [childContainerNode,]);
        state.DataflowDiagramMapping.Remove(childContainer);

        foreach (var connector in childContainer.GetConnectors())
        {
            foreach (var affectedLink in connector.Links)
                state.DataflowDiagramMapping.Remove(affectedLink);
        }
    }

    private void RepositionNodes(List<NodeModel> newNodes, Point target)
    {
        var nodeCenter = newNodes.GetBounds().Center;
        var deltaX = target.X - nodeCenter.X;
        var deltaY = target.Y - nodeCenter.Y;

        foreach (var node in newNodes)
        {
            node.SetPosition(node.Position.X + deltaX, node.Position.Y + deltaY);

            if (node is ChildContainerNode ccNode)
                ChildContainerMapper.UpdatePosition(state, ccNode);
            else if (node is FunctionBlockNode fbNode)
                FunctionBlockMapper.UpdatePosition(state, fbNode);
            else if (node is LabelNode labelNode)
                LabelMapper.UpdatePosition(state, labelNode);
        }
    }
}
