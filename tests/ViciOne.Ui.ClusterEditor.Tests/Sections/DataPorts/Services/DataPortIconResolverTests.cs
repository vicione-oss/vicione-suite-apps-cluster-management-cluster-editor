using System;
using System.Linq;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortIconResolverTests
{
    private readonly Guid _childId = Guid.NewGuid();
    private readonly DataPortTreeNode _dataPortTreeNode;
    private readonly IDatastore _datastore = Substitute.For<IDatastore>();
    private readonly DataPortIconResolver _resolver;
    private readonly TreeBuilder.TreeBuilder _treeBuilder = new(Resources.TestResources.MqttRuleset);

    public DataPortIconResolverTests()
    {
        _resolver = new(_datastore);

        var builder = Substitute.For<IClusterBuilder>();
        var builderCache = Substitute.For<IClusterCache>();
        _dataPortTreeNode = new()
        {
            Id = _childId
        };
        _dataPortTreeNode.IncomingLinks.Add(new());
        _dataPortTreeNode.OutgoingLinks.Add(new());
        builderCache.DataPortTreeNodeIds[_childId].Returns(_dataPortTreeNode);
        builder.Cache.Returns(builderCache);
        _datastore.Builder.Returns(builder);
    }

    private static DataPortRootNodeModel CreateRoot(TreeBuilder.TreeBuilder treeBuilder, string? icon = null, params string[] availableIcons)
        => new()
        {
            AvailableIcons = availableIcons,
            Builder = treeBuilder,
            Icon = icon,
            Name = "Root",
        };

    private static DataPortChildNodeModel CreateChild(DataPortRootNodeModel root, Guid id, string? icon, NodeReference? nodeReference)
        => new()
        {
            Icon = icon,
            Id = new(id),
            Name = "Child",
            NodeReference = nodeReference,
            Parent = root,
            Properties = [
                new DataPortTreeNodeSystemProperty<Cluster.Model.DataPortTransferMode>() { Name = nameof(Cluster.Model.DataPortTransferMode), TypedValue = Cluster.Model.DataPortTransferMode.OnChange },
                new DataPortTreeNodeSystemProperty<string>() { Name = nameof(DataPortTreeNode.ValueType), TypedValue = "String" }
            ],
            RootNode = root,
            TransferDirections = [],
        };

    [Fact]
    public void GetIcons_WithNonDataPortNode_ReturnsEmpty()
    {
        // Arrange
        var node = Substitute.For<ITreeNode>();

        // Act
        var result = _resolver.GetIcons(node);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetIcons_WithRootNode_UsesConfiguredIcon()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder, icon: "broker");
        var expectedIconMarkup = _treeBuilder.GetSvgIcon("broker");

        // Act
        var result = _resolver.GetIcons(root).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }

    [Fact]
    public void GetIcons_WithRootNode_EmptyIcon_UsesFirstAvailableIcon()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder, icon: null, "broker", "folder");
        var expectedIconMarkup = _treeBuilder.GetSvgIcon("broker");

        // Act
        var result = _resolver.GetIcons(root).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }

    [Fact]
    public void GetIcons_WithRootNode_EmptyIcon_ReturnsEmptyListIfNoAvailableIcons()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder, null);

        // Act
        var result = _resolver.GetIcons(root).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetIcons_WithRootNode_FallsBackToServerIcon()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder, icon: "missing");
        var expectedIconMarkup = _treeBuilder.GetSvgIcon("server");

        // Act
        var result = _resolver.GetIcons(root).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }

    [Fact]
    public void GetIcons_WithChildNode_NullNodeReference_ReturnsEmpty()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder);
        var child = CreateChild(root, _childId, icon: "any", nodeReference: null);

        // Act
        var result = _resolver.GetIcons(child);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("datapoint")]
    [InlineData("DATAPOINT")]
    [InlineData("DataPoint")]
    [InlineData("DaTaPoInT")]
    public void GetIcons_WithChildNode_DatapointIcon_ReturnsCorrectIcon(string iconName)
    {
        // Arrange
        var root = CreateRoot(_treeBuilder);
        var child = CreateChild(root, _childId, icon: iconName, nodeReference: new NodeReference { Id = "id" });
        child.IsDataPoint = true;
        var expectedIconMarkup = ColoredIconFactory.GetDataPortIcon("rgb(90, 93, 187)", DataPortDirection.InOut, true, true, 24);

        // Act
        var result = _resolver.GetIcons(child).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, result[0].MarkupString);
    }

    [Fact]
    public void GetIcons_WithChildNode_CustomIcon_ReturnsSvgIcon()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder);
        var child = CreateChild(root, _childId, icon: "folder", nodeReference: new NodeReference { Id = "id" });
        var expectedIconMarkup = _treeBuilder.GetSvgIcon("folder");

        // Act
        var result = _resolver.GetIcons(child).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }

    [Fact]
    public void GetIcons_WithChildNode_NullIcon_UsesNodeTypeIcon()
    {
        // Arrange
        var root = CreateRoot(_treeBuilder);
        var descriptor = root.GetPossibleChildNodes().First(d => d.Children.Count == 0);
        var nodeType = root.Builder.NodeTypes[descriptor.NodeReference.Id];
        var iconName = nodeType.Icons.FirstOrDefault();
        Assert.NotNull(iconName);
        var expectedIconMarkup = _treeBuilder.GetSvgIcon(iconName);

        var child = CreateChild(root, _childId, icon: null, nodeReference: descriptor.NodeReference);

        // Act
        var result = _resolver.GetIcons(child).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }
}
