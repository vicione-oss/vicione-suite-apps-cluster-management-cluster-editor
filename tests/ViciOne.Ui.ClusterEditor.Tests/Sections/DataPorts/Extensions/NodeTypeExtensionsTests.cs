using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public class NodeTypeExtensions_GetInheritedTransferDirections
{
    [Fact]
    public void Can_handle_root_node_as_parent()
    {
        NodeType nodeType = new()
        {
            TransferDirections = [],
        };
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(TestResources.MqttRuleset),
            DisplayText = string.Empty,
        };

        var result = nodeType.GetInheritedTransferDirections(rootNode);

        Assert.Empty(result);
    }

    [Fact]
    public void Returns_transfer_directions_current_node()
    {
        NodeType nodeType = new()
        {
            TransferDirections = [DataPortTransferDirection.Outbound],
        };
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(TestResources.MqttRuleset),
            DisplayText = string.Empty,
        };
        DataPortChildNodeModel parentNode = new()
        {
            DisplayText = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [],
        };

        var result = nodeType.GetInheritedTransferDirections(parentNode);

        Assert.Contains(DataPortTransferDirection.Outbound, result);
    }

    [Fact]
    public void Returns_transfer_directions_from_parent()
    {
        NodeType nodeType = new()
        {
            TransferDirections = [],
        };
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(TestResources.MqttRuleset),
            DisplayText = string.Empty,
        };
        DataPortChildNodeModel parentNode = new()
        {
            DisplayText = string.Empty,
            Parent = rootNode,
            Properties = [],
            RootNode = rootNode,
            TransferDirections = [DataPortTransferDirection.Outbound],
        };

        var result = nodeType.GetInheritedTransferDirections(parentNode);

        Assert.Contains(DataPortTransferDirection.Outbound, result);
    }
}
