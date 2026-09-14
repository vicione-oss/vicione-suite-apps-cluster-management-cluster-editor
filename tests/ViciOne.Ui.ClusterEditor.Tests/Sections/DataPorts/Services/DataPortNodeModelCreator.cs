using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Tests.Resources;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

internal static class DataPortNodeModelCreator
{
    internal static DataPortChildNodeModel CreateDataPortChildNodeModel(
        DataPortNodeModel? parent = null,
        DataPortRootNodeModel? rootNode = null,
        string? icon = "child-icon",
        NodeReference? nodeReference = null,
        bool isDataPoint = true,
        Cluster.Model.DataPortTransferMode dataPortTransferMode = Cluster.Model.DataPortTransferMode.None,
        string? dataTypeValue = null,
        DataPortDirection? dataPortDirection = null,
        List<Cluster.Model.DataPortTransferMode>? availableTransferModes = null,
        IList<DataPortTransferDirection>? transferDirections = null,
        IList<DataPortTransferDirection>? linkDirections = null)
    {
        var properties = new List<IDataPortNodeModelProperty>
        {
            new DataPortTreeNodeSystemProperty<Cluster.Model.DataPortTransferMode>
            {
                AvailableValues = availableTransferModes ?? [.. Enum.GetValues<Cluster.Model.DataPortTransferMode>()],
                DefaultValue = dataPortTransferMode,
                Name = nameof(DataPortTreeNode.TransferMode),
                Value = dataPortTransferMode,
            }
        };

        if (dataTypeValue is not null)
        {
            properties.Add(new DataPortTreeNodeSystemProperty<string>
            {
                Name = nameof(DataPortTreeNode.ValueType),
                TypedDefaultValue = dataTypeValue,
                TypedValue = dataTypeValue,
            });
        }

        if (dataPortDirection is not null)
        {
            properties.Add(new DataPortTreeNodeSystemProperty<DataPortDirection>
            {
                AvailableValues = [DataPortDirection.In, DataPortDirection.Out, DataPortDirection.InOut],
                Name = nameof(DataPort.Direction),
                TypedDefaultValue = dataPortDirection.Value,
                TypedValue = dataPortDirection.Value,
            });
        }

        return new()
        {
            Icon = icon,
            IsDataPoint = isDataPoint,
            // Linkable wherever it transfers unless a test narrows it, matching a node type
            // that declares no LinkDirections of its own.
            LinkDirections = linkDirections ?? transferDirections ?? [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
            Name = "Child",
            NodeReference = nodeReference,
            Parent = parent is null ? CreateDataPortRootNodeModel() : parent,
            Properties = properties,
            RootNode = rootNode is null ? CreateDataPortRootNodeModel() : rootNode,
            // Unrestricted by default, matching a node type that declares no TransferDirections
            // of its own; pass an explicit value to simulate a node narrowed below its parent.
            TransferDirections = transferDirections ?? [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
        };
    }

    internal static DataPortRootNodeModel CreateDataPortRootNodeModel(string? icon = "root-icon")
        => new()
        {
            Builder = new Tree.Builder.TreeBuilder(TestResources.MqttRuleset),
            Icon = icon,
            Name = "Root"
        };
}
