using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Blazor.Diagrams.Core.Models;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Models.Data;

internal sealed class DataflowDiagramMapping
{
    private readonly Dictionary<IConnector, BlockNodeConnector> _blockNodeConnectorMap = [];
    private readonly Dictionary<ChildContainer, ChildContainerNode> _childContainerNodeMap = [];
    private readonly Dictionary<Link, BlockNodeLink> _functionBlockNodeLinkMap = [];
    private readonly Dictionary<FunctionBlock, FunctionBlockNode> _functionBlockNodeMap = [];
    private readonly Dictionary<Label, LabelNode> _labelNodeMap = [];

    public void Add(ChildContainer childContainer, ChildContainerNode node)
    {
        _childContainerNodeMap.Add(childContainer, node);

        foreach (var inputConnector in childContainer.ProcessDataInputs)
            Add(inputConnector, node.GetDataConnector(inputConnector.Index, true)!);

        foreach (var outputConnector in childContainer.ProcessDataOutputs)
            Add(outputConnector, node.GetDataConnector(outputConnector.Index, false)!);
    }

    private void Add(IConnector connector, BlockNodeConnector node)
        => _blockNodeConnectorMap.Add(connector, node);

    public void Add(FunctionBlock functionBlock, FunctionBlockNode node)
    {
        _functionBlockNodeMap.Add(functionBlock, node);

        uint idx = 0;
        foreach (var systemInput in functionBlock.SystemInputs)
            Add(systemInput, node.GetSystemConnector(idx++, true)!);

        idx = 0;
        foreach (var systemOutput in functionBlock.SystemOutputs)
            Add(systemOutput, node.GetSystemConnector(idx++, false)!);

        idx = 0;
        foreach (var dataInput in functionBlock.ProcessDataInputs)
            Add(dataInput, node.GetDataConnector(idx++, true)!);

        idx = 0;
        foreach (var dataOutput in functionBlock.ProcessDataOutputs)
            Add(dataOutput, node.GetDataConnector(idx++, false)!);
    }

    public void Add(Label label, LabelNode node)
        => _labelNodeMap.Add(label, node);

    public void Add(Link link, BlockNodeLink node)
        => _functionBlockNodeLinkMap.Add(link, node);

    public void Clear()
    {
        _blockNodeConnectorMap.Clear();
        _childContainerNodeMap.Clear();
        _functionBlockNodeLinkMap.Clear();
        _functionBlockNodeMap.Clear();
        _labelNodeMap.Clear();
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

        return _blockNodeConnectorMap.First(kvp => kvp.Key.GetUnderlyingConnector() == connector).Value;
    }

    public FunctionBlockNode GetDiagramModel(FunctionBlock functionBlock)
        => _functionBlockNodeMap[functionBlock];

    public IEnumerable<ChildContainerNode> GetDiagramModels(IEnumerable<ChildContainer> childContainers)
        => _childContainerNodeMap
            .Where(kvp => childContainers.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

    public IEnumerable<BlockNodeConnector> GetDiagramModels(IEnumerable<IConnector> connectors)
        => connectors.Select(GetDiagramModel);

    public IEnumerable<FunctionBlockNode> GetDiagramModels(IEnumerable<FunctionBlock> functionBlocks)
        => _functionBlockNodeMap
            .Where(kvp => functionBlocks.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

    public IEnumerable<BlockNodeLink> GetDiagramModels(IEnumerable<Link> links)
        => _functionBlockNodeLinkMap
            .Where(kvp => links.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

    public IEnumerable<LabelNode> GetDiagramModels(IEnumerable<Label> labels)
        => _labelNodeMap
            .Where(kvp => labels.Contains(kvp.Key))
            .Select(kvp => kvp.Value);

    public IEnumerable<BlockNodeConnector> GetInputNodeConnectors()
        => _blockNodeConnectorMap
            .Where(kvp => kvp.Key is IConnectorInput)
            .Select(kvp => kvp.Value);

    public IEnumerable<LabelNode> GetLabelDiagramModels()
        => _labelNodeMap.Values;

    public IEnumerable<Label> GetLabelModels()
        => _labelNodeMap.Keys;

    public IConnector GetModel(BlockNodeConnector connector)
        => _blockNodeConnectorMap.First(kvp => kvp.Value == connector).Key;

    public INamedContainerChild GetModel(BlockNode node)
    {
        var childContainer = _childContainerNodeMap.FirstOrDefault(kvp => kvp.Value == node);
        if (childContainer.Key is not null)
            return childContainer.Key;

        return _functionBlockNodeMap.First(kvp => kvp.Value == node).Key;
    }

    public ChildContainer GetModel(ChildContainerNode node)
        => _childContainerNodeMap.First(kvp => kvp.Value == node).Key;

    public FunctionBlock GetModel(FunctionBlockNode node)
        => _functionBlockNodeMap.First(kvp => kvp.Value == node).Key;

    public Link GetModel(BlockNodeLink link)
        => _functionBlockNodeLinkMap.First(kvp => kvp.Value == link).Key;

    public Label GetModel(LabelNode node)
        => _labelNodeMap.First(kvp => kvp.Value == node).Key;

    public IEnumerable<IConnector> GetModels(IEnumerable<BlockNodeConnector> connectors)
        => _blockNodeConnectorMap
            .Where(kvp => connectors.Contains(kvp.Value))
            .Select(kvp => kvp.Key);

    public IEnumerable<ChildContainer> GetModels(IEnumerable<ChildContainerNode> nodes)
        => _childContainerNodeMap
            .Where(kvp => nodes.Contains(kvp.Value))
            .Select(kvp => kvp.Key);

    public IEnumerable<FunctionBlock> GetModels(IEnumerable<FunctionBlockNode> nodes)
        => _functionBlockNodeMap
            .Where(kvp => nodes.Contains(kvp.Value))
            .Select(kvp => kvp.Key);

    public IEnumerable<Link> GetModels(IEnumerable<BlockNodeLink> links)
        => _functionBlockNodeLinkMap
            .Where(kvp => links.Contains(kvp.Value))
            .Select(kvp => kvp.Key);

    public IEnumerable<Label> GetModels(IEnumerable<LabelNode> nodes)
        => _labelNodeMap
            .Where(kvp => nodes.Contains(kvp.Value))
            .Select(kvp => kvp.Key);

    public IEnumerable<BlockNodeConnector> GetNodeConnectors()
        => _blockNodeConnectorMap.Values;

    public IEnumerable<BlockNodeLink> GetNodeLinks()
        => _functionBlockNodeLinkMap.Values;

    public IEnumerable<NodeModel> GetNodes()
        => _functionBlockNodeMap
            .Select(kvp => kvp.Value as NodeModel)
            .Concat(_labelNodeMap.Values)
            .Concat(_childContainerNodeMap.Values);

    public IEnumerable<BlockNodeConnector> GetOutputNodeConnectors()
        => _blockNodeConnectorMap
            .Where(kvp => kvp.Key is IConnectorOutput)
            .Select(kvp => kvp.Value);

    public void Remove(ChildContainer container)
    {
        var connectors = _blockNodeConnectorMap.Keys
            .OfType<ContainerConnector>()
            .Where(c => c.Container == container)
            .ToList();

        foreach (var connector in connectors)
            Remove(connector);

        _childContainerNodeMap.Remove(container);
    }

    public void Remove(IConnector connector)
        => _blockNodeConnectorMap.Remove(connector);

    public void Remove(FunctionBlock functionBlock)
    {
        foreach (var connector in functionBlock.GetConnectors())
            Remove(connector);

        _functionBlockNodeMap.Remove(functionBlock);
    }

    public void Remove(Label label)
        => _labelNodeMap.Remove(label);

    public void Remove(Link link)
        => _functionBlockNodeLinkMap.Remove(link);

    public bool TryGetDiagramModel(ChildContainer childContainer, [MaybeNullWhen(false)] out ChildContainerNode childContainerNode)
        => _childContainerNodeMap.TryGetValue(childContainer, out childContainerNode);

    public bool TryGetDiagramModel(IConnector connector, [MaybeNullWhen(false)] out BlockNodeConnector nodeConnector)
    {
        if (_blockNodeConnectorMap.TryGetValue(connector, out nodeConnector))
            return true;

        nodeConnector = _blockNodeConnectorMap.FirstOrDefault(kvp => kvp.Key.GetUnderlyingConnector() == connector).Value;
        return nodeConnector is not null;
    }

    public bool TryGetDiagramModel(FunctionBlock functionBlock, [MaybeNullWhen(false)] out FunctionBlockNode functionBlockNode)
        => _functionBlockNodeMap.TryGetValue(functionBlock, out functionBlockNode);

    public bool TryGetDiagramModel(Label label, [MaybeNullWhen(false)] out LabelNode labelNode)
        => _labelNodeMap.TryGetValue(label, out labelNode);

    public bool TryGetDiagramModel(Link link, [MaybeNullWhen(false)] out BlockNodeLink blockNodeLink)
        => _functionBlockNodeLinkMap.TryGetValue(link, out blockNodeLink);
}
