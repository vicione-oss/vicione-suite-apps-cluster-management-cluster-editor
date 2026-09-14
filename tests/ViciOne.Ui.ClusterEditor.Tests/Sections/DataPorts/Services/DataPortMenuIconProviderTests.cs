using System.Linq;
using AwesomeAssertions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.Rules;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using ViciOne.Ui.ColorableIcons;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public class DataPortMenuIconProviderTests
{
    private static DataPortChildNodeContextMenuDescriptor DescriptorUnder(DataPortNodeModel parent, string nodeTypeId)
        => parent.GetPossibleChildNodes().Single(d => d.NodeReference.Id == nodeTypeId);

    private static DataPortChildNodeModel CreateDataPoint(DataPortDirection direction, string nodeTypeId = "DataPointFloat")
        => CreateDataPoint(TestResources.MqttEnvelopeChildrenRuleset, direction, nodeTypeId);

    private static DataPortChildNodeModel CreateDataPoint(Ruleset ruleset, DataPortDirection direction, string nodeTypeId)
    {
        DataPortRootNodeModel rootNode = new()
        {
            Builder = new(ruleset),
            Name = "MQTT",
        };

        var broker = CreateNode(rootNode, rootNode.GetPossibleChildNodes().Single());
        broker.GetRequiredSystemProperty<DataPortDirection>().Value = direction;

        return CreateNode(broker, DescriptorUnder(broker, nodeTypeId));
    }

    private static DataPortChildNodeModel CreateNode(DataPortNodeModel parent, DataPortChildNodeContextMenuDescriptor descriptor)
    {
        var node = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, parent);
        parent.Children.Add(node);
        return node;
    }

    [Fact]
    public void Menu_icon_of_an_envelope_child_narrowed_to_outbound_shows_only_the_outbound_arrow()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "EngineCycle"));

        // Assert
        icon.Should().NotBeNull();
        icon.Should().NotContain(IconParts.DataPortArrowInPath);
        icon.Should().Contain(IconParts.DataPortArrowOutPath);
    }

    /// <summary>
    /// The menu used to take its arrows from the DataPort's own direction, so an outbound-only
    /// child was offered with an inbound arrow it lost the moment it was inserted.
    /// </summary>
    [Fact]
    public void Menu_icon_of_an_envelope_child_that_transfers_nowhere_shows_no_arrows_and_is_disabled()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.In);

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Validity"));

        // Assert
        icon.Should().NotBeNull();
        icon.Should().NotContain(IconParts.DataPortArrowInPath);
        icon.Should().NotContain(IconParts.DataPortArrowOutPath);
        icon.Should().Contain(DataPortColors.Disabled);
    }

    [Fact]
    public void Menu_icon_of_a_datapoint_follows_the_configured_port_direction()
    {
        // Arrange
        var broker = CreateDataPoint(DataPortDirection.In).Parent;

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(broker, "DataPointFloat"));

        // Assert
        icon.Should().NotBeNull();
        icon.Should().Contain(IconParts.DataPortArrowInPath);
        icon.Should().NotContain(IconParts.DataPortArrowOutPath);
    }

    /// <summary>
    /// An envelope child's own transfer mode is always fixed to None, so it is greyed out exactly
    /// when the datapoint carrying it is - and the menu has to say so before the node is created.
    /// </summary>
    [Fact]
    public void Menu_icon_of_an_envelope_child_under_a_switched_off_datapoint_is_disabled()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);
        dataPoint.GetRequiredSystemProperty<Cluster.Model.DataPortTransferMode>(nameof(DataPortTreeNode.TransferMode)).Value
            = Cluster.Model.DataPortTransferMode.None;

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Timestamp"));

        // Assert
        icon.Should().Contain(DataPortColors.Disabled);
    }

    [Fact]
    public void Menu_icon_of_an_envelope_child_under_a_sending_datapoint_is_not_disabled()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);
        dataPoint.GetRequiredSystemProperty<Cluster.Model.DataPortTransferMode>(nameof(DataPortTreeNode.TransferMode)).Value
            = Cluster.Model.DataPortTransferMode.OnChange;

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Timestamp"));

        // Assert
        icon.Should().NotContain(DataPortColors.Disabled);
    }

    [Fact]
    public void Menu_icon_of_a_predefined_envelope_child_is_drawn_as_connected()
    {
        // Arrange: a child that can never be linked is always carrying its parent's value, so the
        // menu must not offer it as something still waiting to be wired up.
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Validity"));

        // Assert
        icon.Should().Be(ColoredIconFactory.GetDataPortIcon(
            ConnectorColor.Get(typeof(bool)), DataPortDirection.Out, isConnectedToOutputConnectors: true));
    }

    [Fact]
    public void Menu_icon_of_a_linkable_node_that_is_not_linked_yet_is_not_drawn_as_connected()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);
        var color = ConnectorColor.Get(typeof(System.DateTime));

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Timestamp"));

        // Assert
        icon.Should().Be(ColoredIconFactory.GetDataPortIcon(color, DataPortDirection.InOut));
        icon.Should().NotBe(ColoredIconFactory.GetDataPortIcon(color, DataPortDirection.InOut, true, true));
    }

    [Fact]
    public void Menu_icon_of_a_marker_envelope_child_falls_back_to_the_node_type_icon()
    {
        // Arrange
        var dataPoint = CreateDataPoint(TestResources.EnvelopeChildrenEdgeCasesRuleset, DataPortDirection.Out, "DataPointOutboundOnly");

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(dataPoint, "Marker"));

        // Assert
        icon.Should().Be(dataPoint.RootNode.Builder.GetSvgIcon("datapoint"));
    }

    [Fact]
    public void Menu_icon_of_a_folder_is_the_plain_node_type_icon()
    {
        // Arrange
        var broker = CreateDataPoint(DataPortDirection.InOut).Parent;

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(DescriptorUnder(broker, "Folder"));

        // Assert
        icon.Should().Be(broker.GetRootNode().Builder.GetSvgIcon("folder"));
    }

    [Fact]
    public void Menu_icon_of_a_namespace_grouping_entry_is_empty()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);
        var descriptor = new DataPortChildNodeContextMenuDescriptor
        {
            IconName = string.Empty,
            Name = "Some namespace",
            NodeReference = new Tree.Builder.NodeTypes.NodeReference { Id = "Not.A.Node.Type" },
            ParentNode = dataPoint,
        };

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(descriptor);
        var iconData = DataPortMenuIconProvider.GetIconData(descriptor);

        // Assert
        icon.Should().BeNull();
        iconData.Should().BeEmpty();
    }

    [Fact]
    public void Menu_icon_matches_the_datapoint_icon_name_whatever_its_casing()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);
        var descriptor = DescriptorUnder(dataPoint, "Timestamp");
        var mixedCase = new DataPortChildNodeContextMenuDescriptor
        {
            IconName = "DaTaPoInT",
            IsDataPoint = descriptor.IsDataPoint,
            Name = descriptor.Name,
            NodeReference = descriptor.NodeReference,
            ParentNode = descriptor.ParentNode,
        };

        // Act
        var icon = DataPortMenuIconProvider.GetIcon(mixedCase);

        // Assert
        icon.Should().Be(DataPortMenuIconProvider.GetIcon(descriptor));
    }

    [Fact]
    public void Menu_icon_data_is_a_base64_svg_sized_for_the_menu()
    {
        // Arrange
        var dataPoint = CreateDataPoint(DataPortDirection.InOut);

        // Act
        var iconData = DataPortMenuIconProvider.GetIconData(DescriptorUnder(dataPoint, "Timestamp"));

        // Assert
        iconData.Should().StartWith("data:image/svg+xml;base64,");
        var svg = System.Text.Encoding.UTF8.GetString(
            System.Convert.FromBase64String(iconData["data:image/svg+xml;base64,".Length..]));
        svg.Should().Contain("viewBox=\"4 4 28 28\" width=\"16\" height=\"16\"");
        svg.Should().NotContain("currentColor");
    }
}
