using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortClusterEventSynchronizerTests : IAsyncDisposable
{
    private readonly ClusterBuilderEventBuffer _buffer = new();
    private readonly ITreeBuilder _builder;
    private readonly IDatastore _datastore;
    private readonly IRulesetProvider _rulesetProvider;
    private readonly DataPortTreeState _state;
    private readonly DataPortClusterEventSynchronizer _synchronizer;

    public DataPortClusterEventSynchronizerTests()
    {
        _datastore = Substitute.For<IDatastore>();
        _rulesetProvider = Substitute.For<IRulesetProvider>();
        _builder = Substitute.For<ITreeBuilder>();
        _state = new DataPortTreeState { Builder = _builder };
        _synchronizer = new DataPortClusterEventSynchronizer(_buffer, _rulesetProvider, _datastore, _state);
    }

    public async ValueTask DisposeAsync()
    {
        _synchronizer.Dispose();
        _buffer.Dispose();
        _builder.Dispose();
        await _datastore.DisposeAsync();
    }

    private void Raise<T>(string eventName, T payload)
    {
        var field = typeof(ClusterBuilderEventBuffer).GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"No backing field found for event '{eventName}'.");

        if (field.GetValue(_buffer) is Action<T> handler)
            handler(payload);
    }

    [Fact]
    public void OnDataPortsRemoved_WhenNodeNotFound_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        var dataPort = new DataPort { Name = "Port" };
        var dataflow = new Cluster.Model.Dataflow();

        // Act
        Raise<IEnumerable<(Cluster.Model.Dataflow Parent, DataPort DataPort)>>(
            nameof(ClusterBuilderEventBuffer.DataPortsRemoved),
            [(dataflow, dataPort)]);

        // Assert
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyRootNodesChanged();
    }

    [Fact]
    public void OnDataPortTreeNodeLinksChanged_WhenDeletionInProgress_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        _state.IsDeletionInProgress = true;

        // Act
        Raise<IEnumerable<Link>>(
            nameof(ClusterBuilderEventBuffer.DataPortTreeNodeLinksAdded),
            [new Link()]);

        // Assert
        _builder.Notifications.ReceivedCalls();
        Assert.Empty(_builder.Notifications.ReceivedCalls());
    }

    private static DataPortChildNodeModel AddChild(DataPortRootNodeModel root, Guid id, DataPortNodeModel? parent = null)
    {
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(
            parent: parent ?? root,
            rootNode: root);
        child.Id = new GuidNodeIdentifier(id);
        (parent ?? root).Children.Add(child);
        return child;
    }

    [Fact]
    public void OnDataPortsRemoved_WhenRootHasRemainingChildren_NotifiesRootNodeAndChildren()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var dataPort = new DataPort { Name = "Port" };
        var removedChild = AddChild(root, dataPort.Id);
        AddChild(root, Guid.NewGuid());

        // Act
        Raise<IEnumerable<(Cluster.Model.Dataflow Parent, DataPort DataPort)>>(
            nameof(ClusterBuilderEventBuffer.DataPortsRemoved),
            [(new Cluster.Model.Dataflow(), dataPort)]);

        // Assert
        Assert.DoesNotContain(removedChild, root.Children);
        Assert.Contains(root, _state.RootNodes);
        _builder.Notifications.Received().NotifyChildrenChanged(root);
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyRootNodesChanged();
    }

    [Fact]
    public void OnDataPortsRemoved_WhenRootHasNoRemainingChildren_RemovesRootAndClearsEditingNode()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var dataPort = new DataPort { Name = "Port" };
        var removedChild = AddChild(root, dataPort.Id);
        _state.EditingTreeNode = removedChild;

        // Act
        Raise<IEnumerable<(Cluster.Model.Dataflow Parent, DataPort DataPort)>>(
            nameof(ClusterBuilderEventBuffer.DataPortsRemoved),
            [(new Cluster.Model.Dataflow(), dataPort)]);

        // Assert
        Assert.DoesNotContain(root, _state.RootNodes);
        Assert.Null(_state.EditingTreeNode);
        _builder.Notifications.Received().NotifyRootNodesChanged();
    }

    [Fact]
    public void OnDataPortPropertiesChanged_WhenDeletionInProgress_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        _state.IsDeletionInProgress = true;

        // Act
        Raise<IEnumerable<(object? sender, PropertyChangedEventArgs e)>>(
            nameof(ClusterBuilderEventBuffer.DataPortPropertiesChanged),
            [(new DataPort { Name = "Port" }, new PropertyChangedEventArgs("Direction"))]);

        // Assert
        Assert.Empty(_builder.Notifications.ReceivedCalls());
    }

    [Fact]
    public void OnDataPortPropertiesChanged_WhenPropertyIsNotDirectionOrSenderIsNotDataPort_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();

        // Act
        Raise<IEnumerable<(object? sender, PropertyChangedEventArgs e)>>(
            nameof(ClusterBuilderEventBuffer.DataPortPropertiesChanged),
            [
                (new DataPort { Name = "Port" }, new PropertyChangedEventArgs("Name")),
                (new object(), new PropertyChangedEventArgs("Direction"))
            ]);

        // Assert
        Assert.Empty(_builder.Notifications.ReceivedCalls());
    }

    [Fact]
    public void OnDataPortPropertiesChanged_WhenDirectionChangedForKnownNode_UpdatesIcons()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var dataPort = new DataPort { Name = "Port" };
        var child = AddChild(root, dataPort.Id);
        AddChild(root, Guid.NewGuid(), parent: child);

        // Act
        Raise<IEnumerable<(object? sender, PropertyChangedEventArgs e)>>(
            nameof(ClusterBuilderEventBuffer.DataPortPropertiesChanged),
            [(dataPort, new PropertyChangedEventArgs("Direction"))]);

        // Assert - the node and its nested child both get an icon-change notification.
        _builder.Notifications.Received().NotifyNodeChanged(child, ChangedNodeDetail.Icons);
    }

    [Fact]
    public void OnDataPortPropertiesChanged_WhenDirectionChangedForUnknownNode_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var unknownDataPort = new DataPort { Name = "Unknown" };

        // Act
        Raise<IEnumerable<(object? sender, PropertyChangedEventArgs e)>>(
            nameof(ClusterBuilderEventBuffer.DataPortPropertiesChanged),
            [(unknownDataPort, new PropertyChangedEventArgs("Direction"))]);

        // Assert - no matching node, so nothing is notified.
        Assert.Empty(_builder.Notifications.ReceivedCalls());
    }

    [Fact]
    public void OnTreeNodesRemoved_WhenChildFoundWithChildParent_RemovesAndNotifies()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var parent = AddChild(root, Guid.NewGuid());
        var child = AddChild(root, Guid.NewGuid(), parent: parent);
        _state.EditingTreeNode = child;
        var treeNode = new DataPortTreeNode { Id = child.Id.Value };
        _state.IsDeletionInProgress = true;

        // Act
        Raise<IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)>>(
            nameof(ClusterBuilderEventBuffer.TreeNodesRemoved),
            [(null!, treeNode)]);

        // Assert
        Assert.DoesNotContain(child, parent.Children);
        Assert.Null(_state.EditingTreeNode);
        _builder.Notifications.Received().NotifyChildrenChanged(parent);
        Assert.False(_state.IsDeletionInProgress);
    }

    [Fact]
    public void OnTreeNodesRemoved_WhenParentIsNotChildNode_DoesNotRemove()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var child = AddChild(root, Guid.NewGuid());
        var treeNode = new DataPortTreeNode { Id = child.Id.Value };
        _state.IsDeletionInProgress = true;

        // Act
        Raise<IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)>>(
            nameof(ClusterBuilderEventBuffer.TreeNodesRemoved),
            [(null!, treeNode)]);

        // Assert - parent is the root node (not a DataPortChildNodeModel), so nothing is removed.
        Assert.Contains(child, root.Children);
        Assert.False(_state.IsDeletionInProgress);
    }

    [Fact]
    public void OnTreeNodesRemoved_WhenNodeNotFound_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        var treeNode = new DataPortTreeNode { Id = Guid.NewGuid() };
        _state.IsDeletionInProgress = true;

        // Act
        Raise<IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)>>(
            nameof(ClusterBuilderEventBuffer.TreeNodesRemoved),
            [(null!, treeNode)]);

        // Assert - no matching node, so nothing is notified.
        Assert.Empty(_builder.Notifications.ReceivedCalls());
        Assert.False(_state.IsDeletionInProgress);
    }

    [Fact]
    public void OnDataPortTreeNodeLinksChanged_WhenSourceNodeFound_NotifiesIcons()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var child = AddChild(root, Guid.NewGuid());
        var link = new Link { SourceDataPortTreeNode = new DataPortTreeNode { Id = child.Id.Value } };

        // Act
        Raise<IEnumerable<Link>>(
            nameof(ClusterBuilderEventBuffer.DataPortTreeNodeLinksAdded),
            [link]);

        // Assert
        _builder.Notifications.Received().NotifyNodeChanged(child, ChangedNodeDetail.Icons);
    }

    [Fact]
    public void OnDataPortTreeNodeLinksChanged_WhenDestinationNodeFound_NotifiesIcons()
    {
        // Arrange
        _synchronizer.Initialize();
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        _state.AddRootNode(root);
        var child = AddChild(root, Guid.NewGuid());
        var link = new Link { DestinationDataPortTreeNode = new DataPortTreeNode { Id = child.Id.Value } };

        // Act
        Raise<IEnumerable<Link>>(
            nameof(ClusterBuilderEventBuffer.DataPortTreeNodeLinksAdded),
            [link]);

        // Assert
        _builder.Notifications.Received().NotifyNodeChanged(child, ChangedNodeDetail.Icons);
    }

    [Fact]
    public void OnDataPortTreeNodeLinksChanged_WhenNodeNotFound_DoesNotNotify()
    {
        // Arrange
        _synchronizer.Initialize();
        var link = new Link { SourceDataPortTreeNode = new DataPortTreeNode { Id = Guid.NewGuid() } };

        // Act
        Raise<IEnumerable<Link>>(
            nameof(ClusterBuilderEventBuffer.DataPortTreeNodeLinksAdded),
            [link]);

        // Assert
        Assert.Empty(_builder.Notifications.ReceivedCalls());
    }

    [Fact]
    public void Dispose_AfterInitialize_UnsubscribesWithoutError()
    {
        // Arrange
        _synchronizer.Initialize();

        // Act & Assert (should not throw)
        _synchronizer.Dispose();
    }
}
