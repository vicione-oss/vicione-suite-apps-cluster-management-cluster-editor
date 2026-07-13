using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using Connector = ViciOne.Cluster.Model.Connector;
using FunctionBlock = ViciOne.Cluster.Model.FunctionBlock;
using Link = ViciOne.Cluster.Model.Link;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Answers read-only connectivity questions about the active container: connected
/// links/nodes in a direction, valid drop targets for a set of connectors, and the
/// visible connector model behind a diagram port. Depends only on the read-only
/// <see cref="IDatastoreState"/>.
/// </summary>
internal sealed class LinkQueryService(IDatastoreState state)
{
    private Dictionary<FunctionBlock, List<Link>> BuildLinksByFbIndex(ConnectionDirection direction)
    {
        var linksByFb = new Dictionary<FunctionBlock, List<Link>>();
        foreach (var link in state.Builder.Cache.Links)
        {
            var fb = direction == ConnectionDirection.Predecessor
                ? link.DestinationConnector?.FunctionBlock
                : link.SourceConnector?.FunctionBlock;

            if (fb is null)
                continue;

            if (!linksByFb.TryGetValue(fb, out var list))
            {
                list = [];
                linksByFb[fb] = list;
            }

            list.Add(link);
        }

        return linksByFb;
    }

    public IEnumerable<Link> GetConnectedLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
    {
        if (blockNode is FunctionBlockNode fbNode)
            return GetConnectedLinks(state.DataflowDiagramMapping.GetModel(fbNode), direction, depth);

        if (blockNode is ChildContainerNode contNode)
            return GetConnectedLinks(state.DataflowDiagramMapping.GetModel(contNode), direction, depth);

        return [];
    }

    private HashSet<Link> GetConnectedLinks(FunctionBlock functionBlock, ConnectionDirection direction, int? depth)
        => GetConnectedLinks(functionBlock, direction, depth, BuildLinksByFbIndex(direction));

    private HashSet<Link> GetConnectedLinks(FunctionBlock functionBlock, ConnectionDirection direction, int? depth, Dictionary<FunctionBlock, List<Link>> linksByFb)
    {
        var result = new HashSet<Link>();
        var maxDepth = depth ?? int.MaxValue;
        var queue = new Queue<(int, FunctionBlock)>();
        var visited = new HashSet<FunctionBlock>();
        queue.Enqueue((1, functionBlock));

        while (queue.Count > 0)
        {
            var (currentDepth, fb) = queue.Dequeue();
            if (currentDepth > maxDepth)
                continue;

            if (!linksByFb.TryGetValue(fb, out var connectedLinks))
                continue;

            foreach (var link in connectedLinks)
            {
                result.Add(link);
                var nextFb = direction == ConnectionDirection.Predecessor ? link.SourceConnector?.FunctionBlock : link.DestinationConnector?.FunctionBlock;
                if (nextFb is not null && nextFb.Container == state.ActiveContainer && visited.Add(nextFb))
                    queue.Enqueue((currentDepth + 1, nextFb));
            }
        }

        return result;
    }

    private HashSet<Link> GetConnectedLinks(ChildContainer childContainer, ConnectionDirection direction, int? depth)
    {
        var linksByFb = BuildLinksByFbIndex(direction);
        var result = new HashSet<Link>();

        foreach (var connector in childContainer.GetConnectors())
            result.UnionWith(GetConnectedLinks(connector.FunctionBlock, direction, depth, linksByFb));

        return result;
    }

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(BlockNode blockNode, ConnectionDirection direction, int? depth)
        => state.DataflowDiagramMapping.GetDiagramModels(GetConnectedLinks(blockNode, direction, depth));

    public IEnumerable<BlockNodeLink> GetConnectedNodeLinks(IEnumerable<BlockNode> blockNodes)
    {
        var links = new HashSet<BlockNodeLink>();

        foreach (var node in blockNodes)
        {
            foreach (var link in GetConnectedNodeLinks(node, ConnectionDirection.Predecessor, 1))
                links.Add(link);

            foreach (var link in GetConnectedNodeLinks(node, ConnectionDirection.Successor, 1))
                links.Add(link);
        }

        return links;
    }

    public IEnumerable<BlockNode> GetConnectedNodes(IEnumerable<BlockNode> blockNodes)
    {
        var nodes = blockNodes.ToArray();
        var inputLinks = new HashSet<Link>();
        var outputLinks = new HashSet<Link>();

        foreach (var node in nodes)
        {
            if (node is FunctionBlockNode fbNode)
            {
                var functionBlock = state.DataflowDiagramMapping.GetModel(fbNode);
                foreach (var input in functionBlock.GetAllInputs())
                {
                    foreach (var link in input.Links)
                        inputLinks.Add(link);
                }

                foreach (var output in functionBlock.GetAllOutputs())
                {
                    foreach (var link in output.Links)
                        outputLinks.Add(link);
                }
            }
            else if (node is ChildContainerNode contNode)
            {
                var container = state.DataflowDiagramMapping.GetModel(contNode);
                foreach (var connector in container.GetConnectors())
                {
                    if (connector is ContainerConnectorInput input)
                    {
                        foreach (var link in input.Links)
                            inputLinks.Add(link);
                    }
                    else if (connector is ContainerConnectorOutput output)
                    {
                        foreach (var link in output.Links)
                            outputLinks.Add(link);
                    }
                }
            }
        }

        return GetConnectedNodes(inputLinks, outputLinks);
    }

    private List<BlockNode> GetConnectedNodes(HashSet<Link> inputLinks, HashSet<Link> outputLinks)
    {
        var connectedFbs = new HashSet<FunctionBlock>();
        var connectedContainers = new HashSet<ChildContainer>();

        foreach (var link in inputLinks)
        {
            if (link.SourceConnector is null)
                continue;

            if (link.SourceConnector.FunctionBlock.Container == state.ActiveContainer)
            {
                connectedFbs.Add(link.SourceConnector.FunctionBlock);
            }
            else
            {
                var upstreamContainer = link.SourceConnector.GetAllUpstreamContainerConnectors()
                    .FirstOrDefault(cc => cc.Container.Parent == state.ActiveContainer)?.Container;
                if (upstreamContainer is not null)
                    connectedContainers.Add(upstreamContainer);
            }
        }

        foreach (var link in outputLinks)
        {
            if (link.DestinationConnector is null)
                continue;

            if (link.DestinationConnector.FunctionBlock.Container == state.ActiveContainer)
            {
                connectedFbs.Add(link.DestinationConnector.FunctionBlock);
            }
            else
            {
                var upstreamContainer = link.DestinationConnector.GetAllUpstreamContainerConnectors()
                    .FirstOrDefault(cc => cc.Container.Parent == state.ActiveContainer)?.Container;
                if (upstreamContainer is not null)
                    connectedContainers.Add(upstreamContainer);
            }
        }

        var result = new List<BlockNode>(connectedFbs.Count + connectedContainers.Count);
        result.AddRange(state.DataflowDiagramMapping.GetDiagramModels(connectedFbs));
        result.AddRange(state.DataflowDiagramMapping.GetDiagramModels(connectedContainers));
        return result;
    }

    public IEnumerable<BlockNodeConnector> GetValidTargetConnectors(IEnumerable<Connector> connectors, bool visibleLink)
    {
        var connectorArray = connectors as Connector[] ?? [.. connectors];

        if (connectorArray.Length == 0)
            return [];

        var isInputConnector = connectorArray[0] is IConnectorInput;

        var targetConnectors = isInputConnector
                ? state.DataflowDiagramMapping.GetOutputNodeConnectors().Select(state.DataflowDiagramMapping.GetModel)
                : state.DataflowDiagramMapping.GetInputNodeConnectors().Select(state.DataflowDiagramMapping.GetModel);

        targetConnectors = isInputConnector
            ? targetConnectors.Where(c => connectorArray.Any(sc => state.Builder.Editors.Connector.CanCreateLink((IConnectorOutput)c, (IConnectorInput)sc, visibleLink)))
            : targetConnectors.Where(c => connectorArray.Any(sc => state.Builder.Editors.Connector.CanCreateLink((IConnectorOutput)sc, (IConnectorInput)c, visibleLink)));

        return [.. targetConnectors.Select(state.DataflowDiagramMapping.GetDiagramModel)];
    }

    public IConnector GetVisibleConnectorModel(BlockNodeConnector blockNodeConnector)
    {
        if (blockNodeConnector.Node is ChildContainerNode)
        {
            var conModel = state.DataflowDiagramMapping.GetModel(blockNodeConnector);
            var upstreamContainerConnectors = conModel.GetUnderlyingConnector().GetAllUpstreamContainerConnectors();
            return upstreamContainerConnectors.First(cc => cc.Container.Parent == state.ActiveContainer);
        }
        else
        {
            return state.DataflowDiagramMapping.GetModel(blockNodeConnector);
        }
    }

    public IEnumerable<IConnector> GetVisibleConnectorModels(IEnumerable<BlockNodeConnector> blockNodeConnectors)
        => blockNodeConnectors.Select(GetVisibleConnectorModel);
}
