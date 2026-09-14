using System;
using System.Linq;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Icons;
using ViciOne.Tree.Builder.NodeTypes;
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
    private readonly Tree.Builder.TreeBuilder _treeBuilder = new(Resources.TestResources.MqttRuleset);

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

    private static DataPortRootNodeModel CreateRoot(Tree.Builder.TreeBuilder treeBuilder, string? icon = null)
        => new()
        {
            Builder = treeBuilder,
            Icon = icon,
            Name = "Root",
        };

    private static DataPortChildNodeModel CreateChild(DataPortRootNodeModel root, Guid id, string? icon, NodeReference? nodeReference)
        => new()
        {
            Icon = icon,
            Id = new(id),
            LinkDirections = [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
            Name = "Child",
            NodeReference = nodeReference,
            Parent = root,
            Properties = [
                new DataPortTreeNodeSystemProperty<Cluster.Model.DataPortTransferMode>()
                {
                    AvailableValues = [Cluster.Model.DataPortTransferMode.OnChange, Cluster.Model.DataPortTransferMode.Periodic, Cluster.Model.DataPortTransferMode.None],
                    Name = nameof(Cluster.Model.DataPortTransferMode),
                    TypedValue = Cluster.Model.DataPortTransferMode.OnChange,
                },
                new DataPortTreeNodeSystemProperty<string>() { Name = nameof(DataPortTreeNode.ValueType), TypedValue = "String" }
            ],
            RootNode = root,
            // Unrestricted by default, matching a node type that declares no TransferDirections
            // of its own.
            TransferDirections = [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
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
    public void GetIcons_WithRootNode_EmptyIcon_UsesTheRulesetRootIcon()
    {
        // Arrange: a root node with no icon of its own falls back to the one its ruleset root
        // declares. Set here rather than taken from the fixture, whose 'mqtt' icon is not in the
        // icon library and would land in the server fallback covered below.
        var treeBuilder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset);
        treeBuilder.Ruleset.Root!.Icon = new IconReference { Id = "broker" };
        var root = CreateRoot(treeBuilder, icon: null);
        var expectedIconMarkup = treeBuilder.GetSvgIcon("broker");

        // Act
        var result = _resolver.GetIcons(root).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
    }

    [Fact]
    public void GetIcons_WithRootNode_EmptyIcon_AndARulesetRootWithoutAnIcon_ReturnsEmpty()
    {
        // Arrange: the ruleset is validated before its icon is cleared, since a root without one
        // would not pass validation - this covers the resolver, not the ruleset.
        var treeBuilder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset);
        treeBuilder.Ruleset.Root!.Icon = new IconReference();
        var root = CreateRoot(treeBuilder, null);

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
    public void GetIcons_WithChildNode_DatapointIconButNotADataPoint_FallsBackToNodeTypeIcon()
    {
        // Arrange: a marker envelope child (no DataTypes) still carries the "datapoint" icon
        // name, but GetDataPointIcon has nothing to draw for it since it is not a datapoint.
        // It must still get a sensible icon instead of none at all.
        var root = CreateRoot(_treeBuilder);
        var child = CreateChild(root, _childId, icon: "datapoint", nodeReference: new NodeReference { Id = "DataPointBool" });
        child.IsDataPoint = false;
        var expectedIconMarkup = _treeBuilder.GetSvgIcon("datapoint");

        // Act
        var result = _resolver.GetIcons(child).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedIconMarkup, ((SvgIcon)result[0]).MarkupString);
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
        var iconName = nodeType.Icon.GetName();
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
