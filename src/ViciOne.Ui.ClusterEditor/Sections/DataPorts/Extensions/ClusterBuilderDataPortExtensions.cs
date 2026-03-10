using System;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder;
using DataPortTransferMode = ViciOne.Cluster.Model.DataPortTransferMode;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class ClusterBuilderDataPortExtensions
{
    public static void EnsureSystemDataPortDependencyExists(this IClusterBuilder builder, IRulesetProvider rulesetProvider)
    {
        // Ensure the SystemDataPortDependency gets added independent of dataPortType
        var systemDataPortDependency = rulesetProvider.GetSystemDataPortDependency();
        if (!builder.Cache.ClusterDependencies.Contains(systemDataPortDependency))
        {
            builder.Editors.Cluster.AddDependency(systemDataPortDependency.Name, systemDataPortDependency.Version);
        }
    }

    public static DataPort GetDataPort(this IClusterBuilder builder, DataPortChildNodeModel dataPortNode)
    {
        var successor = dataPortNode.GetRootSuccessor();
        var dataPortId = successor.Id.Value;
        if (!builder.Cache.DataPortGuids.TryGetValue(dataPortId, out var dataPort))
            throw new InvalidOperationException($"DataPort {dataPortId} not found.");

        return dataPort;
    }

    private static NodeType GetNodeTypeOrThrow(DataPortRootNodeModel rootNode, DataPortChildNodeModel dataPortNode)
    {
        ArgumentNullException.ThrowIfNull(dataPortNode.NodeReference, nameof(dataPortNode.NodeReference));
        var nodeType = rootNode.Builder.NodeTypes[dataPortNode.NodeReference.Id];
        ArgumentNullException.ThrowIfNull(nodeType, nameof(dataPortNode.NodeReference));

        return nodeType;
    }

    public static DataPort GetOrCreateDataPort(this IClusterBuilder builder, IDatastore datastore, DataPortRootNodeModel rootNode,
        DataPortChildNodeModel dataPortNode)
    {
        var nodeType = GetNodeTypeOrThrow(rootNode, dataPortNode);

        var dataPortId = dataPortNode.Id.Value;
        var nodeTransferDirection = dataPortNode.GetSystemProperty<DataPortDirection>()?.TypedValue
            ?? DataPortDirection.Out;

        if (!builder.Cache.DataPortGuids.TryGetValue(dataPortId, out var dataPort))
        {
            var dataPortType = rootNode.Builder.Ruleset.Root?.Id
                ?? throw new InvalidOperationException("The Builder ruleset for root is not set.");

            dataPort = builder.Editors.Dataflow.AddDataPort(
                datastore.ActiveDataflow,
                nodeType.Id,
                dataPortNode.DisplayText,
                nodeTransferDirection,
                dataPortType,
                dataPortId
            );

            var defaultIcon = dataPortNode.Icon ?? dataPortNode.AvailableIcons.FirstOrDefault();
            if (defaultIcon is not null)
                builder.Editors.DataPort.SetIcon(dataPort, defaultIcon);

            // builder changes the name if it already exists "MQTT Broker" -> "MQTT Broker 1"
            dataPortNode.DisplayText = dataPort.Name;
        }
        else
        {
            // TODO can it be changed for a created DataPort?
            builder.Editors.DataPort.SetDirection(dataPort, nodeTransferDirection);
        }

        return dataPort;
    }

    public static DataPortTreeNode GetOrCreateDataPortTreeNode(
        this IClusterBuilder builder,
        DataPort dataPort,
        DataPortChildNodeModel childNode)
    {
        var treeNodeId = childNode.Id.Value;
        var treeRoot = childNode.RootNode;
        var parentId = childNode.Parent.Id.Value;

        var nodeType = GetNodeTypeOrThrow(treeRoot, childNode);

        Type? dataType = null;
        var dataTypeName = nodeType.DataTypes.FirstOrDefault();
        if (dataTypeName is not null)
            dataType = treeRoot.Builder.DataTypes[dataTypeName].RuntimeType;

        // parent representing the first root successor => DataPort
        if (parentId == dataPort.Id)
        {
            // first DataPort treenode - get/add data port
            return FixNodeProperties(childNode, dataPort.TreeNodes.FirstOrDefault(n => n.Id == treeNodeId)
                        ?? builder.Editors.DataPort.AddTreeNode(
                        nodeType.Id,
                        dataPort,
                        childNode.DisplayText,
                        dataType,
                        childNode.GetRequiredSystemProperty<DataPortTransferMode>().TypedValue,
                        treeNodeId));
        }

        var treeParent = builder.Cache.DataPortTreeNodes.First(n => n.Id == parentId);

        // get/add the tree node
        return FixNodeProperties(childNode, treeParent.Children.FirstOrDefault(n => n.Id == treeNodeId)
                ?? builder.Editors.DataPortTreeNode.AddTreeNode(
                nodeType.Id,
                treeParent,
                childNode.DisplayText,
                dataType,
                childNode.GetRequiredSystemProperty<DataPortTransferMode>().TypedValue,
                treeNodeId));

        // ClusterBuilder updates the names of the node if it already exists...eg. second Folder -> Folder 1
        static DataPortTreeNode FixNodeProperties(DataPortChildNodeModel target, DataPortTreeNode source)
        {
            target.DisplayText = source.Name;
            return source;
        }
    }

    public static void RemoveUnusedSystemDataPortDependency(this IClusterBuilder builder, IRulesetProvider rulesetProvider)
    {
        // Remove SystemDataPortDependency if we have no DataPorts in the cluster anymore
        if (builder.Cache.DataPorts.Count == 0)
            builder.Editors.Cluster.RemoveDependency(rulesetProvider.GetSystemDataPortDependency());
    }

    public static void SetCustomDataPortProperties(this IClusterBuilder builder, DataPort dataPort, DataPortChildNodeModel dataPortNode)
    {
        foreach (var customProperty in dataPortNode.Properties.OfType<DataPortNodeModelCustomProperty>())
        {
            if (!builder.Cache.DataPortPropertyGuids.TryGetValue(customProperty.Id, out var dataPortProperty))
                dataPortProperty = builder.Editors.DataPort.AddProperty(customProperty.Reference.Id, dataPort, customProperty.Id);

            builder.Editors.DataPortProperty.SetValue(dataPortProperty, customProperty.Value);
        }
    }

    public static void SetCustomDataPortTreeNodeProperties(
        this IClusterBuilder builder,
        DataPortTreeNode clusterTreeNode,
        DataPortChildNodeModel childNode)
    {
        foreach (var customProperty in childNode.Properties.OfType<DataPortNodeModelCustomProperty>())
        {
            if (!builder.Cache.DataPortPropertyGuids.TryGetValue(customProperty.Id, out var dataPortProperty))
                dataPortProperty = builder.Editors.DataPortTreeNode.AddProperty(customProperty.Reference.Id, clusterTreeNode, customProperty.Id);

            builder.Editors.DataPortProperty.SetValue(dataPortProperty, customProperty.Value);
        }
    }

    public static void SetSystemDataPortProperties(this IClusterBuilder builder, DataPort dataPort, DataPortChildNodeModel dataPortNode, ITreeBuilder treeBuilder)
    {
        var directionProperty = dataPortNode.GetSystemProperty<DataPortDirection>();
        if (directionProperty is not null && directionProperty.TypedValue != dataPort.Direction)
        {
            builder.Editors.DataPort.SetDirection(dataPort, directionProperty.TypedValue);
            treeBuilder.Notifications.NotifyChildrenChanged(dataPortNode);
        }

        var descriptionProperty = dataPortNode.GetSystemProperty<string>(nameof(DataPort.Description));
        if (descriptionProperty is not null)
            builder.Editors.DataPort.SetDescription(dataPort, descriptionProperty.TypedValue);
    }

    public static void SetSystemDataPortTreeNodeProperties(
        this IClusterBuilder builder,
        DataPortTreeNode clusterTreeNode,
        DataPortChildNodeModel childNode)
    {
        var transferModeProperty = childNode.GetSystemProperty<DataPortTransferMode>();
        if (transferModeProperty is not null)
            builder.Editors.DataPortTreeNode.SetTransferMode(clusterTreeNode, transferModeProperty.TypedValue);

        var descriptionProperty = childNode.GetSystemProperty<string>(nameof(DataPortTreeNode.Description));
        if (descriptionProperty is not null)
            builder.Editors.DataPortTreeNode.SetDescription(clusterTreeNode, descriptionProperty.TypedValue);

        var transferIntervalInMsProperty = childNode.GetSystemProperty<uint?>(nameof(DataPortTreeNode.TransferIntervalInMs));
        if (transferIntervalInMsProperty is not null)
            builder.Editors.DataPortTreeNode.SetTransferIntervalInMs(clusterTreeNode, transferIntervalInMsProperty.TypedValue);

        var valueTypeProperty = childNode.GetSystemProperty<string>(nameof(DataPortTreeNode.ValueType));
        if (valueTypeProperty is not null && !string.IsNullOrEmpty(valueTypeProperty.TypedValue))
        {
            var dataType = childNode.RootNode.Builder.DataTypes[valueTypeProperty.TypedValue].RuntimeType;
            if (dataType != clusterTreeNode.ValueType)  // TODO: change cluster builder to only throw if the type would change!
                builder.Editors.DataPortTreeNode.SetValueType(clusterTreeNode, dataType);
        }

        // TODO: TransferPooling
    }
}
