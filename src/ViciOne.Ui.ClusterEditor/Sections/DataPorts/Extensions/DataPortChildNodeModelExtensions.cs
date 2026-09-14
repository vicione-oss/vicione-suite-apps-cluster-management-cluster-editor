using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class DataPortChildNodeModelExtensions
{
    public static void AssignValuesAndProperties(this DataPortChildNodeModel targetChildNode, DataPortTreeNode clusterTreeNode, Tree.Builder.TreeBuilder treeBuilder)
    {
        targetChildNode.Name = clusterTreeNode.Name;
        targetChildNode.Icon = clusterTreeNode.Icon ?? targetChildNode.Icon;
        targetChildNode.PossibleChildren = [.. targetChildNode.GetPossibleChildNodes()];

        targetChildNode.SetSystemPropertyValue(clusterTreeNode.Description, nameof(DataPortTreeNode.Description));
        targetChildNode.SetSystemPropertyValue(clusterTreeNode.TransferMode, nameof(DataPortTreeNode.TransferMode));
        targetChildNode.SetSystemPropertyValue(clusterTreeNode.TransferIntervalInMs, nameof(DataPortTreeNode.TransferIntervalInMs));

        var dataTypeName = treeBuilder.GetDataTypeName(clusterTreeNode.ValueType);
        if (!string.IsNullOrEmpty(dataTypeName))
            targetChildNode.SetSystemPropertyValue(dataTypeName, nameof(DataPortTreeNode.ValueType));

        targetChildNode.SetCustomProperties(clusterTreeNode);
    }

    public static void AssignValuesAndProperties(this DataPortChildNodeModel targetChildNode, DataPort dataPort)
    {
        targetChildNode.Name = dataPort.Name;
        targetChildNode.Icon = dataPort.Icon ?? targetChildNode.Icon;
        targetChildNode.PossibleChildren = [.. targetChildNode.GetPossibleChildNodes()];

        targetChildNode.SetSystemPropertyValue(dataPort.Description, nameof(DataPort.Description));
        targetChildNode.SetSystemPropertyValue(dataPort.Direction, nameof(DataPort.Direction));

        targetChildNode.SetCustomProperties(dataPort);
    }

    public static void AssignValuesAndProperties(this DataPortChildNodeModel targetChildNode, DataPortChildNodePropertyValueStore propertyValueStore)
    {
        if (propertyValueStore.TryGet<string>(nameof(DataPortChildNodeModel.Name), out var name))
            targetChildNode.Name = name;

        if (propertyValueStore.TryGet<string>(nameof(DataPortChildNodeModel.Icon), out var icon))
            targetChildNode.Icon = icon;

        foreach (var property in targetChildNode.Properties)
        {
            if (propertyValueStore.TryGet<object?>(property.Name, out var value))
                property.Value = value;
        }
    }

    public static DataPortDirection? GetEffectiveIconDirection(this DataPortChildNodeModel node, DataPortDirection rootDirection)
        => GetEffectiveIconDirection(node.TransferDirections, rootDirection);

    /// <summary>
    /// The same answer for a node that does not exist yet, so the insert menu can draw the icon
    /// the tree will draw once the node is created.
    /// </summary>
    internal static DataPortDirection? GetEffectiveIconDirection(IEnumerable<DataPortTransferDirection> transferDirections, DataPortDirection rootDirection)
    {
        // The icon's arrows must reflect this node's own effective directions (already narrowed
        // to a subset of its parent's by GetInheritedTransferDirections), intersected with the
        // root DataPort's user-configured direction.
        var directions = transferDirections as ICollection<DataPortTransferDirection> ?? [.. transferDirections];

        var hasInbound = directions.Contains(DataPortTransferDirection.Inbound)
            && rootDirection is DataPortDirection.In or DataPortDirection.InOut;
        var hasOutbound = directions.Contains(DataPortTransferDirection.Outbound)
            && rootDirection is DataPortDirection.Out or DataPortDirection.InOut;

        return (hasInbound, hasOutbound) switch
        {
            (true, true) => DataPortDirection.InOut,
            (true, false) => DataPortDirection.In,
            (false, true) => DataPortDirection.Out,
            (false, false) => null,
        };
    }

    /// <summary>
    /// Which sides of the icon are drawn as connected, given what is actually linked.
    /// <para>A side the node transfers in but may never be linked in counts as connected whatever
    /// is attached: a predefined envelope child such as a validity gets its value from its parent,
    /// so the value is always flowing. Drawing it hollow would read as a datapoint nobody wired
    /// up, when in fact nothing can ever be wired to it.</para>
    /// </summary>
    internal static (bool Inbound, bool Outbound) GetEffectiveIconFill(
        DataPortDirection? direction,
        IEnumerable<DataPortTransferDirection> linkDirections,
        bool hasInboundLink,
        bool hasOutboundLink)
    {
        var links = linkDirections as ICollection<DataPortTransferDirection> ?? [.. linkDirections];

        var transfersInbound = direction is DataPortDirection.In or DataPortDirection.InOut;
        var transfersOutbound = direction is DataPortDirection.Out or DataPortDirection.InOut;

        return (hasInboundLink || (transfersInbound && !links.Contains(DataPortTransferDirection.Inbound)),
                hasOutboundLink || (transfersOutbound && !links.Contains(DataPortTransferDirection.Outbound)));
    }

    public static DataPortTreeNodeSystemProperty<TData> GetRequiredSystemProperty<TData>(this DataPortChildNodeModel childNode, string? propertyName = null)
        => childNode.GetSystemProperty<TData>(propertyName) ?? throw new InvalidOperationException($"System property {propertyName ?? typeof(TData).Name} not found.");

    public static DataPortChildNodeModel GetRootSuccessor(this DataPortChildNodeModel dataPortNode)
    {
        if (dataPortNode.Parent is DataPortRootNodeModel)
            return dataPortNode;

        if (dataPortNode.Parent is DataPortChildNodeModel parentNode)
            return parentNode.GetRootSuccessor();

        throw new InvalidOperationException($"DataPortTreeChildNode {dataPortNode.Id} has no root node.");
    }

    public static DataPortTreeNodeSystemProperty<TData>? GetSystemProperty<TData>(this DataPortChildNodeModel childNode, string? propertyName = null)
        => childNode.Properties.OfType<DataPortTreeNodeSystemProperty<TData>>().FirstOrDefault(k => string.IsNullOrEmpty(propertyName) || k.Name == propertyName);

    private static void SetCustomProperties(this DataPortChildNodeModel childNode, IHasDataPortProperties source)
    {
        foreach (var customProperty in childNode.Properties.OfType<DataPortNodeModelCustomProperty>())
        {
            var dpProp = source.Properties
                            .FirstOrDefault(p => p.DesignId == customProperty.Reference.Id);
            if (dpProp is not null)
            {
                customProperty.Id = dpProp.Id;
                customProperty.Value = dpProp.Value;
            }
        }
    }

    private static void SetSystemPropertyValue<TData>(this DataPortChildNodeModel childNode, TData? data, string? propertyName = null)
    {
        var property = childNode.GetSystemProperty<TData>(propertyName) ?? throw new InvalidOperationException($"System property {propertyName ?? typeof(TData).Name} not found.");
        property.Value = typeof(TData).Name.Equals(nameof(String), StringComparison.OrdinalIgnoreCase) && property.AvailableValues.Count == 1 ? property.AvailableValues[0] : data;
    }

    public static bool TransferDirectionIsPossible(this DataPortChildNodeModel node, Connector connector)
    {
        // A marker child without DataTypes (e.g. the "Type" envelope child) has no value of its
        // own and is never linkable, regardless of the transfer directions it inherited.
        if (!node.IsDataPoint)
            return false;

        // Where the value may be linked, which the ruleset states separately from where it travels:
        // a predefined envelope child is transferred but offers no connector at all.
        var isInput = connector is ConnectorInput;

        if (isInput && node.LinkDirections.Contains(DataPortTransferDirection.Inbound))
            return true;
        if (!isInput && node.LinkDirections.Contains(DataPortTransferDirection.Outbound))
            return true;
        return false;
    }
}
