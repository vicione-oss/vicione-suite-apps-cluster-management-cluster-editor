using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Mappers.DiagramMappers;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.Shared.Dx.Services;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Projects domain children (function blocks, child containers, labels and links)
/// into diagram nodes and registers them in the shared <see cref="DataflowDiagramMapping"/>.
/// Also enumerates the links visible within the active container. This is the single
/// seam reused by load, edit and restructure operations.
/// </summary>
internal sealed class DiagramProjectionService
{
    private readonly ComparerService _comparerService;
    private readonly IJSRuntime _jsRuntime;
    private readonly IDatastoreState _state;

    public DiagramProjectionService(ComparerService comparerService, IDatastoreState state, IJSRuntime jsRuntime)
    {
        _comparerService = comparerService;
        _state = state;
        _jsRuntime = jsRuntime;

        // This line adds the IAggregatingPooling to the cache of Shared.Dx.Services.ComparerService
        // which prevents a noticable delay when the user drags the first FunctionBlock from the Library
        // to the diagram
        var _ = _comparerService.GetComparer(typeof(Core.Contracts.DataModel.IAggregatingPooling));
    }

    public async Task<List<ChildContainerNode>> AddChildContainersToMapping(List<ChildContainer> childContainers, DiagramService diagramService, CancellationToken cancellationToken)
    {
        if (childContainers.Count == 0)
            return [];

        var childContainerNames = new List<string>(childContainers.Count);
        foreach (var cc in childContainers)
            childContainerNames.Add(cc.Name);

        var measuredHeights = await _jsRuntime.MeasureNameFieldHeights(childContainerNames, cancellationToken);

        var result = new List<ChildContainerNode>(childContainers.Count);
        for (var i = 0; i < childContainers.Count; i++)
        {
            var node = ChildContainerMapper.CreateNode(_comparerService, _state, diagramService, childContainers[i], measuredHeights[i]);
            _state.DataflowDiagramMapping.Add(childContainers[i], node);

            result.Add(node);
        }

        return result;
    }

    public async Task<List<FunctionBlockNode>> AddFunctionBlocksToMapping(List<FunctionBlock> functionBlocks, DiagramService diagramService, CancellationToken cancellationToken)
    {
        if (functionBlocks.Count == 0)
            return [];

        var functionBlockNames = new List<string>(functionBlocks.Count);
        foreach (var fb in functionBlocks)
            functionBlockNames.Add(fb.Name);

        var measuredHeights = await _jsRuntime.MeasureNameFieldHeights(functionBlockNames, cancellationToken);

        var result = new List<FunctionBlockNode>(functionBlockNames.Count);
        for (var i = 0; i < functionBlockNames.Count; i++)
        {
            var node = FunctionBlockMapper.CreateNode(_comparerService, _state, diagramService, functionBlocks[i], measuredHeights[i]);
            _state.DataflowDiagramMapping.Add(functionBlocks[i], node);

            result.Add(node);
        }

        return result;
    }

    public List<LabelNode> AddLabelsToMapping(List<Label> labels)
    {
        if (labels.Count == 0)
            return [];

        var result = new List<LabelNode>(labels.Count);

        foreach (var label in labels)
        {
            var node = LabelMapper.CreateNode(label);
            _state.DataflowDiagramMapping.Add(label, node);

            result.Add(node);
        }

        return result;
    }

    public List<BlockNodeLink> AddLinksToMapping(List<Link> links)
    {
        if (links.Count == 0)
            return [];

        var result = new List<BlockNodeLink>(links.Count);

        foreach (var link in links)
        {
            var node = LinkMapper.CreateLink(_state, link);
            _state.DataflowDiagramMapping.Add(link, node);

            result.Add(node);
        }

        return result;
    }

    public IEnumerable<Link> GetActiveContainerContainerLinks()
    {
        foreach (var container in _state.ActiveContainer.Containers)
        {
            foreach (var connector in container.GetConnectors())
            {
                if (connector is not ContainerConnectorOutput output)
                    continue;

                foreach (var link in output.GetVisibleLinksConnectedToThis())
                    yield return link;
            }
        }
    }

    public IEnumerable<Link> GetActiveContainerFunctionBlockLinks()
    {
        foreach (var fb in _state.ActiveContainer.FunctionBlocks)
        {
            foreach (var output in fb.GetAllOutputs())
            {
                foreach (var link in output.GetVisibleLinksConnectedToThis())
                    yield return link;
            }
        }
    }

    public async Task UpdateNameFieldHeights(IReadOnlyList<(BlockNode Node, string Name)> nodes, CancellationToken cancellationToken)
    {
        if (nodes.Count == 0)
            return;

        var names = new List<string>(nodes.Count);
        foreach (var (_, name) in nodes)
            names.Add(name);

        var measuredHeights = await _jsRuntime.MeasureNameFieldHeights(names, cancellationToken);
        if (measuredHeights is null || measuredHeights.Length < nodes.Count)
            return;

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i].Node;
            if (node.NameFieldHeight == measuredHeights[i])
                continue;

            node.NameFieldHeight = measuredHeights[i];
            node.UpdateSize();
            node.RefreshAll();
        }
    }
}
