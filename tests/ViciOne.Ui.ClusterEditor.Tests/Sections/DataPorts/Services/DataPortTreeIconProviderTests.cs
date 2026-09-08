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
        Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
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
        Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
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
        Assert.IsType<SvgIcon>(result);
        Assert.Contains(expectedColor, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
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
        Assert.IsType<SvgIcon>(result);

        if (sholdHaveInputArrow)
            Assert.Contains(IconParts.DataPortArrowInPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
        else
            Assert.DoesNotContain(IconParts.DataPortArrowInPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);

        if (shouldHaveOutputArrow)
            Assert.Contains(IconParts.DataPortArrowOutPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
        else
            Assert.DoesNotContain(IconParts.DataPortArrowOutPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
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
        Assert.IsType<SvgIcon>(result);
        Assert.Contains(IconParts.DataPortArrowInPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
        Assert.Contains(IconParts.DataPortArrowOutPath, ((SvgIcon)result).MarkupString, StringComparison.InvariantCulture);
    }

    [Fact]
    public void GetSvgIcon_DelegatesToTreeBuilder()
    {
        // Arrange
        var treeBuilder = new TreeBuilder.TreeBuilder(Resources.TestResources.MqttRuleset);

        // Act
        var result = DataPortTreeIconProvider.GetSvgIcon(treeBuilder, "non-existent-icon");

        // Assert - delegates to the tree builder; an unknown icon simply returns null.
        Assert.Null(result);
    }
}
