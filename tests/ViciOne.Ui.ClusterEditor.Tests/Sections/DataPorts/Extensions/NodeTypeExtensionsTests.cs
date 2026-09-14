using AwesomeAssertions;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public class NodeTypeExtensionsTests
{
    private static DataPortChildNodeModel CreateFolderParent(DataPortRootNodeModel rootNode)
        => new()
        {
            LinkDirections = [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
            Name = string.Empty,
            NodeReference = new NodeReference { Id = "Folder" },
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound],
        };

    private static DataPortRootNodeModel CreateRootNode()
        => new()
        {
            Builder = new(TestResources.MqttRuleset),
            Name = string.Empty,
        };

    [Fact]
    public void GetInheritedTransferDirections_can_handle_root_node_as_parent()
    {
        // Arrange
        NodeType nodeType = new()
        {
            TransferDirections = [],
        };
        var rootNode = CreateRootNode();

        // Act
        var result = nodeType.GetInheritedTransferDirections(rootNode);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetInheritedTransferDirections_returns_own_transfer_direction_when_the_parent_allows_it()
    {
        // Arrange
        NodeType nodeType = new()
        {
            TransferDirections = [DataPortTransferDirection.Outbound],
        };
        var rootNode = CreateRootNode();
        DataPortChildNodeModel parentNode = new()
        {
            LinkDirections = [DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound],
            Name = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound],
        };

        // Act
        var result = nodeType.GetInheritedTransferDirections(parentNode);

        // Assert
        result.Should().Contain(DataPortTransferDirection.Outbound);
    }

    [Fact]
    public void GetInheritedTransferDirections_returns_transfer_directions_from_parent()
    {
        // Arrange
        NodeType nodeType = new()
        {
            TransferDirections = [],
        };
        var rootNode = CreateRootNode();
        DataPortChildNodeModel parentNode = new()
        {
            LinkDirections = [DataPortTransferDirection.Outbound],
            Name = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [DataPortTransferDirection.Outbound],
        };

        // Act
        var result = nodeType.GetInheritedTransferDirections(parentNode);

        // Assert
        result.Should().Contain(DataPortTransferDirection.Outbound);
    }

    [Fact]
    public void GetInheritedTransferDirections_narrows_its_own_transfer_direction_to_the_intersection_with_the_parent()
    {
        // Arrange: EngineCycle declares TransferDirections: [ Outbound ], but its parent datapoint
        // (and therefore ultimately the broker) may be restricted to Inbound only. The effective
        // direction must be the intersection, i.e. empty here, not the child's own declared
        // direction.
        NodeType nodeType = new()
        {
            TransferDirections = [DataPortTransferDirection.Outbound],
        };
        var rootNode = CreateRootNode();
        DataPortChildNodeModel parentNode = new()
        {
            LinkDirections = [DataPortTransferDirection.Inbound],
            Name = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [DataPortTransferDirection.Inbound],
        };

        // Act
        var result = nodeType.GetInheritedTransferDirections(parentNode);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// LinkDirections is a property of every node type, not only of an envelope child, so a plain
    /// datapoint that declares it must be taken at its word.
    /// </summary>
    [Fact]
    public void GetEffectiveLinkDirections_honours_the_link_directions_a_node_type_declares_outside_an_envelope()
    {
        // Arrange
        var rootNode = CreateRootNode();
        NodeType nodeType = new()
        {
            Id = "DataPointBool",
            LinkDirections = [DataPortLinkDirection.Inbound],
        };

        // Act
        var result = nodeType.GetEffectiveLinkDirections(CreateFolderParent(rootNode), rootNode.Builder);

        // Assert
        result.Should().Equal(DataPortTransferDirection.Inbound);
    }

    [Fact]
    public void GetEffectiveLinkDirections_leaves_a_node_type_that_declares_None_unlinkable()
    {
        // Arrange
        var rootNode = CreateRootNode();
        NodeType nodeType = new()
        {
            Id = "DataPointBool",
            LinkDirections = [DataPortLinkDirection.None],
        };

        // Act
        var result = nodeType.GetEffectiveLinkDirections(CreateFolderParent(rootNode), rootNode.Builder);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetEffectiveLinkDirections_falls_back_to_the_transfer_directions_when_a_node_type_declares_none()
    {
        // Arrange
        var rootNode = CreateRootNode();
        NodeType nodeType = new() { Id = "DataPointBool" };

        // Act
        var result = nodeType.GetEffectiveLinkDirections(CreateFolderParent(rootNode), rootNode.Builder);

        // Assert
        result.Should().Equal(DataPortTransferDirection.Inbound, DataPortTransferDirection.Outbound);
    }
}
