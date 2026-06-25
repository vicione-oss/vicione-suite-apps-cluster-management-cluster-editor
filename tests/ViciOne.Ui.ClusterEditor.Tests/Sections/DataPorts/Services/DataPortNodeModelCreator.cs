using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
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
        DataPortDirection? dataPortDirection = null)
    {
        var properties = new List<IDataPortNodeModelProperty>
        {
            new DataPortTreeNodeSystemProperty<Cluster.Model.DataPortTransferMode>
            {
                AvailableValues = [.. Enum.GetValues<Cluster.Model.DataPortTransferMode>()],
                Name = nameof(DataPortTreeNode.TransferMode),
                Value = dataPortTransferMode,
            }
        };

        if (dataTypeValue is not null)
        {
            properties.Add(new DataPortTreeNodeSystemProperty<string>
            {
                Name = nameof(DataPortTreeNode.ValueType),
                TypedValue = dataTypeValue,
            });
        }

        if (dataPortDirection is not null)
        {
            properties.Add(new DataPortTreeNodeSystemProperty<DataPortDirection>
            {
                AvailableValues = [DataPortDirection.In, DataPortDirection.Out, DataPortDirection.InOut],
                Name = nameof(DataPort.Direction),
                TypedValue = dataPortDirection.Value,
            });
        }

        return new()
        {
            Icon = icon,
            IsDataPoint = isDataPoint,
            Name = "Child",
            NodeReference = nodeReference,
            Parent = parent is null ? CreateDataPortRootNodeModel() : parent,
            Properties = properties,
            RootNode = rootNode is null ? CreateDataPortRootNodeModel() : rootNode,
            TransferDirections = []
        };
    }

    internal static DataPortRootNodeModel CreateDataPortRootNodeModel(string? icon = "root-icon", IReadOnlyList<string>? availableIcons = null)
        => new()
        {
            AvailableIcons = availableIcons ?? [],
            Builder = new TreeBuilder.TreeBuilder(TestResources.MqttRuleset),
            Icon = icon,
            Name = "Root"
        };
}
