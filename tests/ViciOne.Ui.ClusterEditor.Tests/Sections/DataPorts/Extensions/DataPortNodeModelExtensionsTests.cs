using System.Collections.Generic;
using AwesomeAssertions;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Extensions;

public sealed class DataPortNodeModelExtensionsTests
{
    private readonly ITreeBuilder _builder = Substitute.For<ITreeBuilder>();

    private static DataPortChildNodeModel CreateNodeWithChild(out DataPortChildNodeModel child)
    {
        var parent = DataPortNodeModelCreator.CreateDataPortChildNodeModel();
        child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: parent, rootNode: parent.RootNode);
        parent.Children.Add(child);
        return parent;
    }

    [Fact]
    public void Notifies_a_leaf_exactly_once()
    {
        // Arrange
        var leaf = DataPortNodeModelCreator.CreateDataPortChildNodeModel();

        // Act
        leaf.NotifyIconsChangedRecursively(_builder);

        // Assert
        _builder.Notifications.Received(1).NotifyNodeChanged(leaf, ChangedNodeDetail.Icons);
    }

    /// <summary>
    /// An envelope child is greyed out exactly when the datapoint carrying it is, so editing the
    /// datapoint has to redraw the child as well.
    /// </summary>
    [Fact]
    public void Notifies_the_node_and_every_node_below_it()
    {
        // Arrange
        var parent = CreateNodeWithChild(out var child);

        // Act
        parent.NotifyIconsChangedRecursively(_builder);

        // Assert
        _builder.Notifications.Received(1).NotifyNodeChanged(parent, ChangedNodeDetail.Icons);
        _builder.Notifications.Received(1).NotifyNodeChanged(child, ChangedNodeDetail.Icons);
    }

    [Fact]
    public void NotifyDescendantsChanged_NotifiesAllDescendantsButNotNodeItself()
    {
        // Arrange
        List<(ITreeNode Node, ChangedNodeDetail Detail)> notifications = [];
        _builder.Notifications
            .When(n => n.NotifyNodeChanged(Arg.Any<ITreeNode>(), Arg.Any<ChangedNodeDetail>()))
            .Do(call => notifications.Add((call.ArgAt<ITreeNode>(0), call.ArgAt<ChangedNodeDetail>(1))));

        var node = CreateNodeWithChild(out var child);
        var grandchild = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: child, rootNode: node.RootNode);
        child.Children.Add(grandchild);

        // Act
        node.NotifyDescendantsChanged(_builder);

        // Assert
        notifications.Should().HaveCount(2)
            .And.Contain((child, ChangedNodeDetail.None))
            .And.Contain((grandchild, ChangedNodeDetail.None));
    }
}
