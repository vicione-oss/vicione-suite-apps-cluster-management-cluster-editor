using System.Linq;
using NSubstitute;
using ViciOne.Tree.Builder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortNodeSorterTests
{
    private static DataPortChildNodeModel CreateChild(string name, bool isParent, DataPortRootNodeModel root, DataPortNodeModel parent, bool isDataPoint = false)
    {
        var node = new DataPortChildNodeModel
        {
            IsDataPoint = isDataPoint,
            LinkDirections = [],
            Name = name,
            Parent = parent,
            Properties = [],
            RootNode = root,
            TransferDirections = [],
        };

        if (isParent)
        {
            node.PossibleChildren =
            [
                new DataPortChildNodeContextMenuDescriptor
                {
                    IconName = "icon",
                    Name = "possible",
                    NodeReference = new NodeReference { Id = "possible-id" },
                    ParentNode = node,
                }
            ];
        }

        return node;
    }

    private static DataPortRootNodeModel CreateRoot()
        => new()
        {
            Builder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };

    [Fact]
    public void SortChildren_OrdersParentNodesBeforeNonParentNodes()
    {
        // Arrange
        var root = CreateRoot();
        var nonParent = CreateChild("AAA", isParent: false, root, root);
        var parent = CreateChild("ZZZ", isParent: true, root, root);
        root.Children.Add(nonParent);
        root.Children.Add(parent);

        // Act
        DataPortNodeSorter.SortChildren(root, sortNonParentChildren: true);

        // Assert
        Assert.Equal(["ZZZ", "AAA"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public void SortChildren_SortsParentNodesAlphaNumerically()
    {
        // Arrange
        var root = CreateRoot();
        var b = CreateChild("Folder10", isParent: true, root, root);
        var a = CreateChild("Folder2", isParent: true, root, root);
        root.Children.Add(b);
        root.Children.Add(a);

        // Act
        DataPortNodeSorter.SortChildren(root, sortNonParentChildren: false);

        // Assert
        Assert.Equal(["Folder2", "Folder10"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public void SortChildren_WhenSortNonParentChildrenFalse_KeepsNonParentOrder()
    {
        // Arrange
        var root = CreateRoot();
        var second = CreateChild("BBB", isParent: false, root, root);
        var first = CreateChild("AAA", isParent: false, root, root);
        root.Children.Add(second);
        root.Children.Add(first);

        // Act
        DataPortNodeSorter.SortChildren(root, sortNonParentChildren: false);

        // Assert
        Assert.Equal(["BBB", "AAA"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public void SortChildren_WhenSortNonParentChildrenTrue_SortsNonParentNodes()
    {
        // Arrange
        var root = CreateRoot();
        var second = CreateChild("BBB", isParent: false, root, root);
        var first = CreateChild("AAA", isParent: false, root, root);
        root.Children.Add(second);
        root.Children.Add(first);

        // Act
        DataPortNodeSorter.SortChildren(root, sortNonParentChildren: true);

        // Assert
        Assert.Equal(["AAA", "BBB"], root.Children.Select(c => c.Name));
    }

    [Fact]
    public void SortNodes_KeepsADatapointThatCanTakeEnvelopeChildrenBelowTheFolders()
    {
        // Arrange
        var root = CreateRoot();
        var dataPoint = CreateChild("Temperature", isParent: true, root, root, isDataPoint: true);
        var folder = CreateChild("Zone", isParent: true, root, root);
        var leaf = CreateChild("Alarm", isParent: false, root, root, isDataPoint: true);

        // Act
        var sorted = DataPortNodeSorter.SortNodes([dataPoint, folder, leaf], false);

        // Assert
        Assert.Equal(["Zone", "Temperature", "Alarm"], sorted.Select(n => n.Name));
    }

    [Fact]
    public void SortNodes_DoesNotTreatAMarkerEnvelopeChildAsAFolder()
    {
        // Arrange: a marker envelope child (e.g. "Type") has no DataTypes and is therefore not a
        // datapoint, but it also has no children of its own, so it must stay a leaf like any other
        // envelope child rather than being sorted with the folders.
        var root = CreateRoot();
        var folder = CreateChild("Zone", isParent: true, root, root);
        var markerChild = CreateChild("Type", isParent: false, root, root, isDataPoint: false);

        // Act
        var sorted = DataPortNodeSorter.SortNodes([markerChild, folder], false);

        // Assert
        Assert.Equal(["Zone", "Type"], sorted.Select(n => n.Name));
    }

    [Fact]
    public void SortNodes_ReturnsParentNodesFirstThenNonParentNodes()
    {
        // Arrange
        var root = CreateRoot();
        var nonParent = CreateChild("AAA", isParent: false, root, root);
        var parent = CreateChild("BBB", isParent: true, root, root);

        // Act
        var result = DataPortNodeSorter.SortNodes([nonParent, parent], sortNonParentChildren: true).ToList();

        // Assert
        Assert.Equal(["BBB", "AAA"], result.Select(c => c.Name));
    }

    [Fact]
    public void SortNodeChildren_WhenNodeHasPossibleChildren_SortsParentSiblingsAndNotifies()
    {
        // Arrange
        var root = CreateRoot();
        var parent = CreateChild("Parent", isParent: true, root, root);
        root.Children.Add(parent);

        // Siblings of the target node under 'parent' - registered in reverse alphabetical order.
        var siblingZ = CreateChild("ZZZ", isParent: true, root, parent);
        var node = CreateChild("AAA", isParent: true, root, parent);
        parent.Children.Add(siblingZ);
        parent.Children.Add(node);

        var builder = Substitute.For<ITreeBuilder>();

        // Act
        DataPortNodeSorter.SortNodeChildren(node, builder);

        // Assert
        Assert.Equal(["AAA", "ZZZ"], parent.Children.Select(c => c.Name));
        builder.Notifications.Received(1).NotifyChildrenChanged(parent);
    }

    [Fact]
    public void SortNodeChildren_WhenNodeHasNoPossibleChildren_DoesNotNotify()
    {
        // Arrange
        var root = CreateRoot();
        var parent = CreateChild("Parent", isParent: true, root, root);
        root.Children.Add(parent);

        var node = CreateChild("Leaf", isParent: false, root, parent);
        parent.Children.Add(node);

        var builder = Substitute.For<ITreeBuilder>();

        // Act
        DataPortNodeSorter.SortNodeChildren(node, builder);

        // Assert
        builder.Notifications.DidNotReceiveWithAnyArgs().NotifyChildrenChanged((ITreeNode)default!);
    }
}
