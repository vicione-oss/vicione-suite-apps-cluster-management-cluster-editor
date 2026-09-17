using AwesomeAssertions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public class DataPortChildNodeModelExtensionsTests
{
    private static DataPortChildNodeModel CreateNode(bool isDataPoint, params DataPortTransferDirection[] directions)
        => CreateNode(isDataPoint, directions, directions);

    private static DataPortChildNodeModel CreateNode(bool isDataPoint, DataPortTransferDirection[] transferDirections, DataPortTransferDirection[] linkDirections)
    {
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(TestResources.MqttRuleset),
            Name = string.Empty,
        };
        return new DataPortChildNodeModel
        {
            IsDataPoint = isDataPoint,
            LinkDirections = linkDirections,
            Name = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = transferDirections,
        };
    }

    [Fact]
    public void Datapoint_with_matching_direction_can_be_linked()
    {
        // Arrange
        var node = CreateNode(isDataPoint: true, DataPortTransferDirection.Inbound);

        // Act
        var result = node.TransferDirectionIsPossible(new ConnectorInput());

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// A predefined envelope child - a validity or an engine cycle the data port derives from the
    /// parent value - is transferred with its parent but offers no connector in either direction.
    /// </summary>
    [Fact]
    public void Predefined_child_is_not_linkable_in_a_direction_it_is_transferred_in()
    {
        // Arrange
        var node = CreateNode(
            isDataPoint: true,
            [DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound],
            []);

        // Act
        var inboundIsPossible = node.TransferDirectionIsPossible(new ConnectorInput());
        var outboundIsPossible = node.TransferDirectionIsPossible(new ConnectorOutput());

        // Assert
        inboundIsPossible.Should().BeFalse();
        outboundIsPossible.Should().BeFalse();
    }

    [Fact]
    public void Marker_child_without_data_types_is_never_linkable_even_with_a_matching_direction()
    {
        // Arrange: a marker child (no DataTypes, e.g. the "Type" envelope child) still inherits
        // its parent's transfer directions, but it has no value of its own and must never be
        // offered as a link target regardless of direction.
        var node = CreateNode(isDataPoint: false, DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound);

        // Act
        var result = node.TransferDirectionIsPossible(new ConnectorInput());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedByEditMode_WhenChildInEditMode_ReturnsFalse()
    {
        // Arrange
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: node);
        child.IsEditModeActive = true;

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedByEditMode_WhenGrandparentInEditMode_ReturnsTrue()
    {
        // Arrange
        var grandparent = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        grandparent.IsEditModeActive = true;
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: grandparent);
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent);

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLockedByEditMode_WhenNodeItselfInEditMode_ReturnsTrue()
    {
        // Arrange
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        node.IsEditModeActive = true;

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLockedByEditMode_WhenNoNodeInEditMode_ReturnsFalse()
    {
        // Arrange
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent);

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedByEditMode_WhenParentInEditMode_ReturnsTrue()
    {
        // Arrange
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        parent.IsEditModeActive = true;
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent);

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLockedByEditMode_WhenSiblingInEditMode_ReturnsFalse()
    {
        // Arrange
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        var sibling = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent);
        sibling.IsEditModeActive = true;
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent);

        // Act
        var result = node.IsLockedByEditMode();

        // Assert
        result.Should().BeFalse();
    }
}
