using System;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ColorableIcons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using Xunit;
using DataPortTransferDirection = ViciOne.Tree.Builder.NodeTypes.DataPortTransferDirection;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeIconProviderTests
{
    private readonly IClusterCache _clusterCache;
    private readonly DataPortTreeNode _dataPortTreeNode = new();

    public DataPortTreeIconProviderTests()
        => _clusterCache = Substitute.For<IClusterCache>();

    [Fact]
    public void GetDataPointIcon_WhenChildNodeIsNotDataPoint_ReturnsNull()
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(isDataPoint: false);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDataPointIcon_WhenDataTypeValueHasValue_UsesRuntimeTypeColor()
    {
        // Arrange
        var dataType = typeof(int);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(dataPortTransferMode: DataPortTransferMode.Periodic, dataTypeValue: dataType.Name);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expectedColor = ConnectorColor.Get(dataType);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        var typed = Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, typed.MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_WhenDataTypeValueIsEmpty_UsesObjectTypeColor()
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(dataPortTransferMode: DataPortTransferMode.Periodic, dataTypeValue: string.Empty);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expectedColor = ConnectorColor.Get(typeof(object));

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        var typed = Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, typed.MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_WhenDataTypeValueIsNull_ReturnsNull()
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetDataPointIcon_WhenNoneIsTheOnlyAvailableTransferModeAndThereIsNoParentDatapoint_UsesDisabledColor()
    {
        // Arrange: a node whose only available mode is None, and with no parent datapoint to
        // inherit a switched-off state from, can never send and must therefore be disabled.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.None,
            dataTypeValue: nameof(Int32),
            availableTransferModes: [DataPortTransferMode.None]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(DataPortColors.Disabled, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_WhenTheOnlyAvailableTransferModeIsOneItCanSendIn_DoesNotUseDisabledColor()
    {
        // Arrange: a node type may declare a single real transfer mode, e.g. TransferModes:
        // [ OnChange ]. That is a mode of its own and must not be mistaken for the fixed None
        // of an envelope child, which would send it looking for a parent to inherit from.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.OnChange,
            dataTypeValue: nameof(Int32),
            availableTransferModes: [DataPortTransferMode.OnChange]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(DataPortColors.Disabled, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_WhenTransferModeIsNone_UsesDisabledColor()
    {
        // Arrange
        var dataType = typeof(int);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(dataPortTransferMode: DataPortTransferMode.None, dataTypeValue: nameof(Int32));
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expectedColor = DataPortColors.Disabled;

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        var typed = Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, typed.MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_EnvelopeChildOfADisabledParent_UsesDisabledColor()
    {
        // Arrange: an envelope child's own TransferMode is always fixed to None (its single
        // available value), so it carries no meaning of its own. Its disabled state must
        // instead follow its parent datapoint, here set to None.
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.None,
            availableTransferModes: [DataPortTransferMode.OnChange, DataPortTransferMode.Periodic, DataPortTransferMode.None]);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            parent: parent,
            rootNode: parent.RootNode,
            dataPortTransferMode: DataPortTransferMode.None,
            dataTypeValue: nameof(Int32),
            availableTransferModes: [DataPortTransferMode.None]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(DataPortColors.Disabled, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_EnvelopeChildOfAnActiveParent_DoesNotUseDisabledColor()
    {
        // Arrange: same envelope child, but the parent datapoint is actively transferring.
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.OnChange,
            availableTransferModes: [DataPortTransferMode.OnChange, DataPortTransferMode.Periodic, DataPortTransferMode.None]);
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            parent: parent,
            rootNode: parent.RootNode,
            dataPortTransferMode: DataPortTransferMode.None,
            dataTypeValue: nameof(Int32),
            availableTransferModes: [DataPortTransferMode.None]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(DataPortColors.Disabled, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_ChildNarrowedToOutbound_ShowsOnlyTheOutboundArrow()
    {
        // Arrange: EngineCycle-like child declaring TransferDirections: [ Outbound ], under a
        // root configured for InOut. The icon must reflect the node's own effective direction,
        // not the root's configured direction alone.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.InOut,
            transferDirections: [DataPortTransferDirection.Outbound]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(IconParts.DataPortArrowInPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
        Assert.Contains(IconParts.DataPortArrowOutPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_ChildWithNoOverlapWithParentDirection_ShowsNoArrows()
    {
        // Arrange: an Outbound-only child under a root configured for Inbound only has no
        // effective direction left and must show neither arrow.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.In,
            transferDirections: [DataPortTransferDirection.Outbound]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(IconParts.DataPortArrowInPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
        Assert.DoesNotContain(IconParts.DataPortArrowOutPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_ChildWithNoOverlapWithParentDirection_UsesDisabledColor()
    {
        // Arrange: the same node that shows no arrows transfers nowhere at all, so it must not
        // be painted like an ordinary datapoint either.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.In,
            transferDirections: [DataPortTransferDirection.Outbound]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(DataPortColors.Disabled, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_ForASideThatCanNeverBeLinked_IsDrawnAsConnected()
    {
        // Arrange: a predefined envelope child - transferred outbound, never linkable. Its value
        // comes from its parent, so it is always carrying data and must not read as unwired.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.InOut,
            transferDirections: [DataPortTransferDirection.Outbound],
            linkDirections: []);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expected = ColoredIconFactory.GetDataPortIcon(
            ConnectorColor.Get(typeof(int)), DataPortDirection.Out, isConnectedToOutputConnectors: true, size: 24);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.Equal(expected, ((SvgIcon)result!).MarkupString);
    }

    [Fact]
    public void GetDataPointIcon_ForALinkableSideWithNothingLinked_StaysUnconnected()
    {
        // Arrange: the control case - same shape, but the side may be linked, so an empty icon is
        // the honest one.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.InOut,
            transferDirections: [DataPortTransferDirection.Outbound],
            linkDirections: [DataPortTransferDirection.Outbound]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expected = ColoredIconFactory.GetDataPortIcon(
            ConnectorColor.Get(typeof(int)), DataPortDirection.Out, size: 24);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.Equal(expected, ((SvgIcon)result!).MarkupString);
    }

    [Fact]
    public void GetDataPointIcon_ForAChildLinkableOnlyInbound_DrawsOnlyTheOutboundSideAsConnected()
    {
        // Arrange: transferred both ways, linkable inbound only. The outbound half is predefined.
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            dataPortTransferMode: DataPortTransferMode.Periodic,
            dataTypeValue: nameof(Int32),
            dataPortDirection: DataPortDirection.InOut,
            transferDirections: [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
            linkDirections: [DataPortTransferDirection.Inbound]);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);
        var expected = ColoredIconFactory.GetDataPortIcon(
            ConnectorColor.Get(typeof(int)), DataPortDirection.InOut, isConnectedToOutputConnectors: true, size: 24);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.Equal(expected, ((SvgIcon)result!).MarkupString);
    }

    [Theory]
    [InlineData(DataPortDirection.In, true, false)]
    [InlineData(DataPortDirection.Out, false, true)]
    [InlineData(DataPortDirection.InOut, true, true)]
    public void GetDataPointIcon_WithDifferentDataPortDirections_ReturnsCorrectIcon(DataPortDirection dataPortDirection, bool sholdHaveInputArrow, bool shouldHaveOutputArrow)
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(dataPortTransferMode: DataPortTransferMode.Periodic, dataTypeValue: nameof(Int32), dataPortDirection: dataPortDirection);
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        var typed = Assert.IsType<SvgIcon>(result);

        if (sholdHaveInputArrow)
            Assert.Contains(IconParts.DataPortArrowInPath, typed.MarkupString, StringComparison.InvariantCulture);
        else
            Assert.DoesNotContain(IconParts.DataPortArrowInPath, typed.MarkupString, StringComparison.InvariantCulture);

        if (shouldHaveOutputArrow)
            Assert.Contains(IconParts.DataPortArrowOutPath, typed.MarkupString, StringComparison.InvariantCulture);
        else
            Assert.DoesNotContain(IconParts.DataPortArrowOutPath, typed.MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetDataPointIcon_WithNoRootSuccessorDirection_UsesDefaultInOut()
    {
        // Arrange
        var childNode = DataPortNodeModelCreator.CreateDataPortChildNodeModel(dataPortTransferMode: DataPortTransferMode.Periodic, dataTypeValue: nameof(Int32));
        _clusterCache.DataPortTreeNodeIds[childNode.Id.Value].Returns(_dataPortTreeNode);

        // Act
        var result = DataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _clusterCache);

        // Assert
        Assert.NotNull(result);
        var typed = Assert.IsType<SvgIcon>(result);
        Assert.Contains(IconParts.DataPortArrowInPath, typed.MarkupString, StringComparison.InvariantCulture);
        Assert.Contains(IconParts.DataPortArrowOutPath, typed.MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetSvgIcon_DelegatesToTreeBuilder()
    {
        // Arrange
        var treeBuilder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset);

        // Act
        var result = DataPortTreeIconProvider.GetSvgIcon(treeBuilder, "non-existent-icon");

        // Assert - delegates to the tree builder; an unknown icon simply returns null.
        Assert.Null(result);
    }
}
