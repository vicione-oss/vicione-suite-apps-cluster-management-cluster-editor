using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Blazor.Diagrams.Core.Models;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models.Data;

public sealed class DataflowDiagramMapping
{
    private readonly Dictionary<IConnector, BlockNodeConnector> _blockNodeConnectorMap = [];
    private readonly Dictionary<ChildContainer, ChildContainerNode> _childContainerNodeMap = [];
    private readonly Dictionary<IConnector, ChildContainer> _connectorContainerMap = [];
    private readonly Dictionary<ChildContainer, List<IConnector>> _containerConnectorsMap = [];
    private readonly Dictionary<Link, BlockNodeLink> _functionBlockNodeLinkMap = [];
    private readonly Dictionary<FunctionBlock, FunctionBlockNode> _functionBlockNodeMap = [];
    private readonly HashSet<BlockNodeConnector> _inputConnectors = [];
    private readonly Dictionary<Label, LabelNode> _labelNodeMap = [];
    private readonly HashSet<BlockNodeConnector> _outputConnectors = [];
    private readonly Dictionary<ChildContainerNode, ChildContainer> _reverseChildContainerMap = [];
    private readonly Dictionary<BlockNodeConnector, IConnector> _reverseConnectorMap = [];
    private readonly Dictionary<FunctionBlockNode, FunctionBlock> _reverseFunctionBlockMap = [];
    private readonly Dictionary<LabelNode, Label> _reverseLabelMap = [];
    private readonly Dictionary<BlockNodeLink, Link> _reverseLinkMap = [];
    private readonly Dictionary<IConnector, BlockNodeConnector> _underlyingConnectorMap = [];

    public void Add(ChildContainer childContainer, ChildContainerNode node)
    {
        _childContainerNodeMap.Add(childContainer, node);
        _reverseChildContainerMap.Add(node, childContainer);

        var connectorList = new List<IConnector>(childContainer.ProcessDataInputs.Count + childContainer.ProcessDataOutputs.Count);

        foreach (var inputConnector in childContainer.ProcessDataInputs)
        {
            var blockNodeConnector = node.GetDataConnector(inputConnector.Index, true)!;
            Add(inputConnector, blockNodeConnector, isInput: true);
            connectorList.Add(inputConnector);
            _connectorContainerMap.Add(inputConnector, childContainer);
        }

        foreach (var outputConnector in childContainer.ProcessDataOutputs)
        {
            var blockNodeConnector = node.GetDataConnector(outputConnector.Index, false)!;
            Add(outputConnector, blockNodeConnector, isInput: false);
            connectorList.Add(outputConnector);
            _connectorContainerMap.Add(outputConnector, childContainer);
        }

        _containerConnectorsMap.Add(childContainer, connectorList);
    }

    private void Add(IConnector connector, BlockNodeConnector node, bool isInput)
    {
        _blockNodeConnectorMap.Add(connector, node);
        _reverseConnectorMap.Add(node, connector);

        if (isInput)
            _inputConnectors.Add(node);
        else
            _outputConnectors.Add(node);

        var underlying = connector.GetUnderlyingConnector();
        if (!ReferenceEquals(underlying, connector))
            _underlyingConnectorMap.TryAdd(underlying, node);
    }

    public void Add(FunctionBlock functionBlock, FunctionBlockNode node)
    {
        _functionBlockNodeMap.Add(functionBlock, node);
        _reverseFunctionBlockMap.Add(node, functionBlock);

        uint idx = 0;
        foreach (var systemInput in functionBlock.SystemInputs)
            Add(systemInput, node.GetSystemConnector(idx++, true)!, isInput: true);

        idx = 0;
        foreach (var systemOutput in functionBlock.SystemOutputs)
            Add(systemOutput, node.GetSystemConnector(idx++, false)!, isInput: false);

        idx = 0;
        foreach (var dataInput in functionBlock.ProcessDataInputs)
            Add(dataInput, node.GetDataConnector(idx++, true)!, isInput: true);

        idx = 0;
        foreach (var dataOutput in functionBlock.ProcessDataOutputs)
            Add(dataOutput, node.GetDataConnector(idx++, false)!, isInput: false);
    }

    public void Add(Label label, LabelNode node)
    {
        _labelNodeMap.Add(label, node);
        _reverseLabelMap.Add(node, label);
    }

    public void Add(Link link, BlockNodeLink node)
    {
        _functionBlockNodeLinkMap.Add(link, node);
        _reverseLinkMap.Add(node, link);
    }

    public void Clear()
    {
        _blockNodeConnectorMap.Clear();
        _childContainerNodeMap.Clear();
        _connectorContainerMap.Clear();
        _containerConnectorsMap.Clear();
        _functionBlockNodeLinkMap.Clear();
        _functionBlockNodeMap.Clear();
        _inputConnectors.Clear();
        _labelNodeMap.Clear();
        _outputConnectors.Clear();
        _reverseChildContainerMap.Clear();
        _reverseConnectorMap.Clear();
        _reverseFunctionBlockMap.Clear();
        _reverseLabelMap.Clear();
        _reverseLinkMap.Clear();
        _underlyingConnectorMap.Clear();

        _blockNodeConnectorMap.TrimExcess();
        _childContainerNodeMap.TrimExcess();
        _connectorContainerMap.TrimExcess();
        _containerConnectorsMap.TrimExcess();
        _functionBlockNodeLinkMap.TrimExcess();
        _functionBlockNodeMap.TrimExcess();
        _inputConnectors.TrimExcess();
        _labelNodeMap.TrimExcess();
        _outputConnectors.TrimExcess();
        _reverseChildContainerMap.TrimExcess();
        _reverseConnectorMap.TrimExcess();
        _reverseFunctionBlockMap.TrimExcess();
        _reverseLabelMap.TrimExcess();
        _reverseLinkMap.TrimExcess();
        _underlyingConnectorMap.TrimExcess();
    }

    public bool ContainsMapping(Link link)
        => _functionBlockNodeLinkMap.ContainsKey(link);

    public IEnumerable<IConnector> GetConnectors()
        => _blockNodeConnectorMap.Keys;

    public ChildContainerNode GetDiagramModel(ChildContainer childContainer)
        => _childContainerNodeMap[childContainer];

    public BlockNodeConnector GetDiagramModel(IConnector connector)
    {
        if (_blockNodeConnectorMap.TryGetValue(connector, out var value))
            return value;

        if (_underlyingConnectorMap.TryGetValue(connector, out value))
            return value;

        throw new KeyNotFoundException($"No mapping found for connector of type {connector.GetType().Name}.");
    }

    public FunctionBlockNode GetDiagramModel(FunctionBlock functionBlock)
        => _functionBlockNodeMap[functionBlock];

    public IEnumerable<ChildContainerNode> GetDiagramModels(IEnumerable<ChildContainer> childContainers)
    {
        foreach (var cc in childContainers)
        {
            if (_childContainerNodeMap.TryGetValue(cc, out var node))
                yield return node;
        }
    }

    public IEnumerable<BlockNodeConnector> GetDiagramModels(IEnumerable<IConnector> connectors)
    {
        foreach (var connector in connectors)
            yield return GetDiagramModel(connector);
    }

    public IEnumerable<FunctionBlockNode> GetDiagramModels(IEnumerable<FunctionBlock> functionBlocks)
    {
        foreach (var fb in functionBlocks)
        {
            if (_functionBlockNodeMap.TryGetValue(fb, out var node))
                yield return node;
        }
    }

    public IEnumerable<BlockNodeLink> GetDiagramModels(IEnumerable<Link> links)
    {
        foreach (var link in links)
        {
            if (_functionBlockNodeLinkMap.TryGetValue(link, out var node))
                yield return node;
        }
    }

    public IEnumerable<LabelNode> GetDiagramModels(IEnumerable<Label> labels)
    {
        foreach (var label in labels)
        {
            if (_labelNodeMap.TryGetValue(label, out var node))
                yield return node;
        }
    }

    public IReadOnlyCollection<BlockNodeConnector> GetInputNodeConnectors()
        => _inputConnectors;

    public IEnumerable<LabelNode> GetLabelDiagramModels()
        => _labelNodeMap.Values;

    public IEnumerable<Label> GetLabelModels()
        => _labelNodeMap.Keys;

    public IConnector GetModel(BlockNodeConnector connector)
        => _reverseConnectorMap[connector];

    public INamedContainerChild GetModel(BlockNode node)
    {
        if (node is ChildContainerNode ccNode && _reverseChildContainerMap.TryGetValue(ccNode, out var cc))
            return cc;

        if (node is FunctionBlockNode fbNode && _reverseFunctionBlockMap.TryGetValue(fbNode, out var fb))
            return fb;

        throw new KeyNotFoundException($"No mapping found for node of type {node.GetType().Name}.");
    }

    public ChildContainer GetModel(ChildContainerNode node)
        => _reverseChildContainerMap[node];

    public FunctionBlock GetModel(FunctionBlockNode node)
        => _reverseFunctionBlockMap[node];

    public Link GetModel(BlockNodeLink link)
        => _reverseLinkMap[link];

    public Label GetModel(LabelNode node)
        => _reverseLabelMap[node];

    public IEnumerable<IConnector> GetModels(IEnumerable<BlockNodeConnector> connectors)
    {
        foreach (var c in connectors)
        {
            if (_reverseConnectorMap.TryGetValue(c, out var model))
                yield return model;
        }
    }

    public IEnumerable<ChildContainer> GetModels(IEnumerable<ChildContainerNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (_reverseChildContainerMap.TryGetValue(n, out var model))
                yield return model;
        }
    }

    public IEnumerable<FunctionBlock> GetModels(IEnumerable<FunctionBlockNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (_reverseFunctionBlockMap.TryGetValue(n, out var model))
                yield return model;
        }
    }

    public IEnumerable<Link> GetModels(IEnumerable<BlockNodeLink> links)
    {
        foreach (var l in links)
        {
            if (_reverseLinkMap.TryGetValue(l, out var model))
                yield return model;
        }
    }

    public IEnumerable<Label> GetModels(IEnumerable<LabelNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (_reverseLabelMap.TryGetValue(n, out var model))
                yield return model;
        }
    }

    public IEnumerable<BlockNodeConnector> GetNodeConnectors()
        => _blockNodeConnectorMap.Values;

    public IEnumerable<BlockNodeLink> GetNodeLinks()
        => _functionBlockNodeLinkMap.Values;

    public IEnumerable<NodeModel> GetNodes()
    {
        foreach (var kvp in _functionBlockNodeMap)
            yield return kvp.Value;
        foreach (var node in _labelNodeMap.Values)
            yield return node;
        foreach (var node in _childContainerNodeMap.Values)
            yield return node;
    }

    public IReadOnlyCollection<BlockNodeConnector> GetOutputNodeConnectors()
        => _outputConnectors;

    public void Remove(ChildContainer container)
    {
        if (_containerConnectorsMap.TryGetValue(container, out var connectors))
        {
            foreach (var connector in connectors)
            {
                _connectorContainerMap.Remove(connector);
                Remove(connector);
            }

            _containerConnectorsMap.Remove(container);
        }

        if (_childContainerNodeMap.TryGetValue(container, out var node))
            _reverseChildContainerMap.Remove(node);

        _childContainerNodeMap.Remove(container);
    }

    public void Remove(IConnector connector)
    {
        if (_connectorContainerMap.TryGetValue(connector, out var ownerContainer))
        {
            _connectorContainerMap.Remove(connector);
            if (_containerConnectorsMap.TryGetValue(ownerContainer, out var list))
            {
                list.Remove(connector);
                if (list.Count == 0)
                    _containerConnectorsMap.Remove(ownerContainer);
            }
        }

        if (_blockNodeConnectorMap.TryGetValue(connector, out var node))
        {
            _reverseConnectorMap.Remove(node);
            _inputConnectors.Remove(node);
            _outputConnectors.Remove(node);
        }

        var underlying = connector.GetUnderlyingConnector();
        if (!ReferenceEquals(underlying, connector))
            _underlyingConnectorMap.Remove(underlying);

        _blockNodeConnectorMap.Remove(connector);
    }

    public void Remove(FunctionBlock functionBlock)
    {
        foreach (var connector in functionBlock.GetConnectors())
            Remove(connector);

        if (_functionBlockNodeMap.TryGetValue(functionBlock, out var node))
            _reverseFunctionBlockMap.Remove(node);

        _functionBlockNodeMap.Remove(functionBlock);
    }

    public void Remove(Label label)
    {
        if (_labelNodeMap.TryGetValue(label, out var node))
            _reverseLabelMap.Remove(node);

        _labelNodeMap.Remove(label);
    }

    public void Remove(Link link)
    {
        if (_functionBlockNodeLinkMap.TryGetValue(link, out var node))
            _reverseLinkMap.Remove(node);

        _functionBlockNodeLinkMap.Remove(link);
    }

    public bool TryGetDiagramModel(ChildContainer childContainer, [MaybeNullWhen(false)] out ChildContainerNode childContainerNode)
        => _childContainerNodeMap.TryGetValue(childContainer, out childContainerNode);

    public bool TryGetDiagramModel(IConnector connector, [MaybeNullWhen(false)] out BlockNodeConnector nodeConnector)
    {
        if (_blockNodeConnectorMap.TryGetValue(connector, out nodeConnector))
            return true;

        return _underlyingConnectorMap.TryGetValue(connector, out nodeConnector);
    }

    public bool TryGetDiagramModel(FunctionBlock functionBlock, [MaybeNullWhen(false)] out FunctionBlockNode functionBlockNode)
        => _functionBlockNodeMap.TryGetValue(functionBlock, out functionBlockNode);

    public bool TryGetDiagramModel(Label label, [MaybeNullWhen(false)] out LabelNode labelNode)
        => _labelNodeMap.TryGetValue(label, out labelNode);

    public bool TryGetDiagramModel(Link link, [MaybeNullWhen(false)] out BlockNodeLink blockNodeLink)
        => _functionBlockNodeLinkMap.TryGetValue(link, out blockNodeLink);
}
