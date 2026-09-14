using System.Linq;
using AwesomeAssertions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using Xunit;
using DataPortTransferMode = ViciOne.Cluster.Model.DataPortTransferMode;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public class ITreeNodeExtensionsTests
{
    private static DataPortChildNodeModel CreateDataPoint(string nodeTypeId)
        => CreateDataPoint(TestResources.MqttEnvelopeChildrenRuleset, nodeTypeId);

    /// <summary>
    /// The shipped dp-mqtt ruleset does not declare every shape a ruleset may take; the edge-case
    /// fixture carries the rest. See <see cref="TestResources.EnvelopeChildrenEdgeCasesRuleset"/>.
    /// </summary>
    private static DataPortChildNodeModel CreateDataPoint(Ruleset ruleset, string nodeTypeId)
    {
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(ruleset),
            Name = "MQTT",
        };
        var broker = CreateNode(rootNode, rootNode.GetPossibleChildNodes().Single());
        return CreateNode(broker, broker.GetPossibleChildNodes().Single(d => d.NodeReference.Id == nodeTypeId));
    }

    private static DataPortChildNodeModel CreateNode(DataPortNodeModel parent, DataPortChildNodeContextMenuDescriptor descriptor)
    {
        var node = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, parent);
        parent.Children.Add(node);
        return node;
    }

    [Fact]
    public void Fixture_ruleset_loads_through_the_validating_TreeBuilder_constructor_without_error()
    {
        // Act
        var act = () => new Tree.Builder.TreeBuilder(TestResources.MqttEnvelopeChildrenRuleset);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Edge_case_fixture_ruleset_loads_through_the_validating_TreeBuilder_constructor_without_error()
    {
        // Act
        var act = () => new Tree.Builder.TreeBuilder(TestResources.EnvelopeChildrenEdgeCasesRuleset);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Datapoint_with_envelope_children_stays_a_datapoint_that_can_have_children()
    {
        // Act
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Assert
        dataPoint.IsDataPoint.Should().BeTrue();
        dataPoint.CanHaveChildren.Should().BeTrue();
    }

    [Fact]
    public void Envelope_child_inherits_the_transfer_directions_of_its_datapoint()
    {
        // Arrange
        var dataPoint = CreateDataPoint(TestResources.EnvelopeChildrenEdgeCasesRuleset, "DataPointOutboundOnly");

        // Act
        var directions = dataPoint.GetPossibleChildNodes()
            .Select(d => DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(d, dataPoint).TransferDirections);

        // Assert
        directions.Should().AllSatisfy(d => d.Should().Equal(DataPortTransferDirection.Outbound));
    }

    [Fact]
    public void Envelope_child_can_declare_its_own_narrower_transfer_directions()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var engineCycle = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == "EngineCycle"), dataPoint);

        // Assert
        engineCycle.TransferDirections.Should().Equal(DataPortTransferDirection.Outbound);
    }

    /// <summary>
    /// Asked through the ruleset's envelope query API: a child that declares
    /// <c>LinkDirections: [ None ]</c> is predefined - transferred with its parent, never linked.
    /// </summary>
    [Theory]
    [InlineData("Validity")]
    [InlineData("EngineCycle")]
    [InlineData("Type")]
    public void Predefined_envelope_child_offers_no_link_direction(string nodeTypeId)
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var child = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == nodeTypeId), dataPoint);

        // Assert
        child.TransferDirections.Should().NotBeEmpty();
        child.LinkDirections.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Timestamp")]
    [InlineData("UserProperty")]
    public void Linkable_envelope_child_is_linkable_wherever_it_is_transferred(string nodeTypeId)
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var child = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == nodeTypeId), dataPoint);

        // Assert
        child.LinkDirections.Should().BeEquivalentTo(child.TransferDirections);
        child.LinkDirections.Should().Contain([DataPortTransferDirection.Outbound, DataPortTransferDirection.Inbound,]);
    }

    /// <summary>
    /// A child under an outbound-only datapoint may not be linked inbound either, however wide
    /// the ruleset declares its own link directions.
    /// </summary>
    [Fact]
    public void Link_directions_of_an_envelope_child_stay_within_the_directions_of_its_datapoint()
    {
        // Arrange
        var dataPoint = CreateDataPoint(TestResources.EnvelopeChildrenEdgeCasesRuleset, "DataPointOutboundOnly");

        // Act
        var timestamp = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == "Timestamp"), dataPoint);

        // Assert
        timestamp.LinkDirections.Should().Equal(DataPortTransferDirection.Outbound);
    }

    [Fact]
    public void Envelope_child_has_no_children_of_its_own()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var children = dataPoint.GetPossibleChildNodes()
            .Select(d => DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(d, dataPoint));

        // Assert
        children.Should().AllSatisfy(child => child.CanHaveChildren.Should().BeFalse());
    }

    [Fact]
    public void Envelope_child_with_data_types_is_a_datapoint()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var children = dataPoint.GetPossibleChildNodes()
            .Select(d => DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(d, dataPoint));

        // Assert
        children.Should().AllSatisfy(child => child.IsDataPoint.Should().BeTrue());
    }

    [Fact]
    public void Envelope_child_without_data_types_is_a_marker_node_and_not_a_datapoint()
    {
        // Arrange
        var dataPoint = CreateDataPoint(TestResources.EnvelopeChildrenEdgeCasesRuleset, "DataPointOutboundOnly");

        // Act
        var markerChild = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == "Marker"), dataPoint);

        // Assert
        markerChild.IsDataPoint.Should().BeFalse();
        markerChild.CanHaveChildren.Should().BeFalse();
        markerChild.GetSystemProperty<string>(nameof(DataPortTreeNode.ValueType)).Should().BeNull();
    }

    [Fact]
    public void Envelope_child_is_no_longer_offered_once_its_maximum_instance_count_is_reached()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");
        CreateNode(dataPoint, dataPoint.GetPossibleChildNodes().Single(d => d.NodeReference.Id == "Timestamp"));

        // Act
        var children = dataPoint.GetPossibleChildNodes().Select(d => d.NodeReference.Id);

        // Assert
        children.Should().NotContain("Timestamp");
    }

    [Fact]
    public void Envelope_child_offers_only_transfer_mode_none()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var modes = dataPoint.GetPossibleChildNodes()
            .Select(d => DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(d, dataPoint))
            .Select(child => child.GetRequiredSystemProperty<DataPortTransferMode>(nameof(DataPortTreeNode.TransferMode)));

        // Assert
        modes.Should().AllSatisfy(mode =>
        {
            mode.AvailableValues.Should().Equal(DataPortTransferMode.None);
            mode.Value.Should().Be(DataPortTransferMode.None);
        });
    }

    [Fact]
    public void Offers_the_envelope_children_declared_under_the_datapoint()
    {
        // Arrange
        var dataPoint = CreateDataPoint("DataPointFloat");

        // Act
        var children = dataPoint.GetPossibleChildNodes().Select(d => d.NodeReference.Id);

        // Assert
        children.Should().BeEquivalentTo(["UserProperty", "Timestamp", "EngineCycle", "Validity", "Type"]);
    }
}
