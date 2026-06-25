using System;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class DataPortChildNodeModelExtensions
{
    public static void AssignValuesAndProperties(this DataPortChildNodeModel targetChildNode, DataPortTreeNode clusterTreeNode, TreeBuilder.TreeBuilder treeBuilder)
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
        var isInput = connector is ConnectorInput;

        if (isInput && node.TransferDirections.Contains(DataPortTransferDirection.Inbound))
            return true;
        if (!isInput && node.TransferDirections.Contains(DataPortTransferDirection.Outbound))
            return true;
        return false;
    }
}
