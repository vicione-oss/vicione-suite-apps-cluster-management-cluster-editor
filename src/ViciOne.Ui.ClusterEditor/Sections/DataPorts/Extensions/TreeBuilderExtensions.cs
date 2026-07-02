using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using DataPortTransferMode = ViciOne.Cluster.Model.DataPortTransferMode;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class TreeBuilderExtensions
{
    public static void CreateDataPortTree(this TreeBuilder.TreeBuilder builder, DataPort dataPort, DataPortRootNodeModel rootNode)
    {
        if (!dataPort.RulesetId.Equals(builder.Ruleset.Root?.Id, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"DataPort ruleset id ({dataPort.RulesetId}) does not match root ruleset id ({builder.Ruleset.Root?.Id})");

        var rootNodeChildren = rootNode.NodeReference switch
        {
            null => builder.Ruleset.Root.ChildNodes,
            _ => builder.NodeTypes.TryGetValue(rootNode.NodeReference.Id, out var rootNodeType)
                ? rootNodeType.ChildNodes
                : throw new InvalidOperationException($"NodeReference with Id {rootNode.NodeReference.Id} is not part of the current ruleset")
        };

        var nodeRef = rootNodeChildren.FirstOrDefault(c => c.Id == dataPort.DesignId)
            ?? throw new InvalidOperationException($"DataPort node reference for DesignId={dataPort.DesignId} not found.");

        if (!builder.NodeTypes.TryGetValue(dataPort.DesignId, out var nodeType))
            throw new InvalidOperationException($"DataPort DesignId={dataPort.DesignId} is not a known type.");

        var dataPortNode = new DataPortChildNodeModel
        {
            AvailableIcons = nodeType.Icons,
            CanHaveChildren = nodeType.ChildNodes.Length != 0,
            Icon = dataPort.Icon,
            Id = new GuidNodeIdentifier(dataPort.Id),
            IsDataPoint = nodeType.IsDataPoint(),
            Name = dataPort.Name,
            NameIsReadOnly = nodeType.NameIsReadOnly,
            NodeReference = nodeRef,
            Parent = rootNode,
            Properties = [.. builder.GetProperties(nodeType)],
            RootNode = rootNode,
            TransferDirections = nodeType.GetInheritedTransferDirections(rootNode),
        };

        rootNode.Children.Add(dataPortNode);

        // create nodes for the tree children
        foreach (var treeNode in dataPort.TreeNodes)
            treeNode.ConvertToEditorNode(rootNode, dataPortNode);

        dataPortNode.AssignValuesAndProperties(dataPort);
    }

    private static IEnumerable<IDataPortNodeModelProperty> GetDataPortProperties(DataPortNodeType dataPortNodeType)
    {
        var availableDirections = TranslateToClusterModel(dataPortNodeType.TransferDirections)
            .Distinct().ToList();
        var defaultDirection = GetDefaultDirection(availableDirections);
        yield return new DataPortTreeNodeSystemProperty<DataPortDirection>
        {
            AvailableValues = availableDirections,
            DefaultValue = defaultDirection,
            Name = nameof(DataPort.Direction),
            Value = defaultDirection,
        };
    }

    private static IEnumerable<IDataPortNodeModelProperty> GetDataPortTreeNodeProperties(DataPortTreeNodeType dataPortTreeNodeType)
    {
        yield return new DataPortTreeNodeSystemProperty<uint?>
        {
            Name = nameof(DataPortTreeNode.TransferIntervalInMs),
            TypedDefaultValue = null,
            TypedValue = null,
        };

        var availableModes = dataPortTreeNodeType.TransferModes
            .Select(TranslateToClusterModel).Distinct().ToList();
        var transferModeProperty = new DataPortTreeNodeSystemProperty<DataPortTransferMode>
        {
            AvailableValues = availableModes,
            DefaultValue = availableModes[0],
            Name = nameof(DataPortTreeNode.TransferMode),
            Value = availableModes[0],
        };
        transferModeProperty.DependentProperties?.Add(nameof(DataPortTreeNode.TransferIntervalInMs), [DataPortTransferMode.Periodic]);
        yield return transferModeProperty;

        if (dataPortTreeNodeType.DataTypes.Length != 0)
        {
            var defaultValue = dataPortTreeNodeType.DataTypes.FirstOrDefault();
            yield return new DataPortTreeNodeSystemProperty<string>
            {
                AvailableValues = [.. dataPortTreeNodeType.DataTypes],
                DefaultValue = defaultValue,
                Name = nameof(DataPortTreeNode.ValueType),
                Value = defaultValue,
            };
        }
    }

    public static string? GetDataTypeName(this TreeBuilder.TreeBuilder builder, Type? dataPortTreeNodeType)
    {
        if (dataPortTreeNodeType is null)
            return null;

        var item = builder.DataTypes.FirstOrDefault(k => k.Value.RuntimeType == dataPortTreeNodeType);

        return item.Value is null ? null : item.Key;
    }

    internal static DataPortDirection GetDefaultDirection(List<DataPortDirection> directions)
    {
        if (directions.Contains(DataPortDirection.InOut))
            return DataPortDirection.InOut;
        if (directions.Contains(DataPortDirection.Out))
            return DataPortDirection.Out;
        return directions[0];
    }

    public static IEnumerable<IDataPortNodeModelProperty> GetProperties(this TreeBuilder.TreeBuilder builder, NodeType nodeType)
    {
        switch (nodeType)
        {
            case DataPortNodeType dataPortNodeType:
                foreach (var property in GetDataPortProperties(dataPortNodeType))
                    yield return property;

                break;

            case DataPortTreeNodeType dataPortTreeNodeType:
                foreach (var property in GetDataPortTreeNodeProperties(dataPortTreeNodeType))
                    yield return property;
                break;
        }

        yield return new DataPortTreeNodeSystemProperty<string>
        {
            DefaultValue = nodeType.Description,
            Name = nameof(nodeType.Description),
            Value = nodeType.Description
        };

        foreach (var propertyCategoryRef in nodeType.PropertyCategories)
        {
            var propertyCategory = builder.Ruleset.PropertyCategoryTypes.First(c => c.Id == propertyCategoryRef.Id);

            foreach (var propertyRef in propertyCategoryRef.Properties)
            {
                var propertyType = builder.PropertyTypes[propertyRef.Id];
                var dataType = builder.DataTypes[propertyType.DataType];
                yield return new DataPortNodeModelCustomProperty()
                {
                    Category = propertyCategory.Name,
                    DefaultValue = propertyType.DefaultValue,
                    DependentProperties = propertyType.DependentProperties,
                    Id = Guid.NewGuid(),
                    MaxValue = propertyType.MaxValue,
                    MinValue = propertyType.MinValue,
                    Name = propertyType.Name,
                    PossibleValues = propertyType.Elements,
                    Reference = propertyRef,
                    RuntimeType = dataType.RuntimeType ?? typeof(object),
                    Type = propertyType,
                    Value = propertyType.DefaultValue,
                };
            }
        }
    }

#pragma warning disable format
    private static IEnumerable<DataPortDirection> TranslateToClusterModel(params DataPortTransferDirection[] treeDataPortTransferModes)
        => treeDataPortTransferModes switch
        {
            [DataPortTransferDirection.Inbound] => [DataPortDirection.In],
            [DataPortTransferDirection.Outbound] => [DataPortDirection.Out],
            [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound]
                or [DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound]
                => [DataPortDirection.In, DataPortDirection.Out, DataPortDirection.InOut],
            _ => []
        };
#pragma warning restore format

    private static DataPortTransferMode TranslateToClusterModel(TreeBuilder.NodeTypes.DataPortTransferMode mode)
        => mode switch
        {
            TreeBuilder.NodeTypes.DataPortTransferMode.None => DataPortTransferMode.None,
            TreeBuilder.NodeTypes.DataPortTransferMode.OnChange => DataPortTransferMode.OnChange,
            TreeBuilder.NodeTypes.DataPortTransferMode.Periodic => DataPortTransferMode.Periodic,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}
