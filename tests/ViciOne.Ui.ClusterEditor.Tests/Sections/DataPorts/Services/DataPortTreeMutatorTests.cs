using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeMutatorTests : IAsyncDisposable
{
    private const string MqttRootId = "MQTTDataPort";

    private readonly ITreeBuilder _builder;
    private readonly IClusterBuilder _clusterBuilder;
    private readonly IDatastore _datastore;
    private readonly DataPortTreeMutator _mutator;
    private readonly DataPortTreeBuilderRegistry _registry;
    private readonly IRulesetProvider _rulesetProvider;
    private readonly DataPortTreeState _state;

    public DataPortTreeMutatorTests()
    {
        _datastore = Substitute.For<IDatastore>();
        _rulesetProvider = Substitute.For<IRulesetProvider>();
        _builder = Substitute.For<ITreeBuilder>();
        _state = new DataPortTreeState { Builder = _builder };
        _registry = new DataPortTreeBuilderRegistry(_rulesetProvider, new FakeLogger<DataPortTreeBuilderRegistry>());
        _mutator = new DataPortTreeMutator(
            _datastore,
            _rulesetProvider,
            Substitute.For<ILogger<DataPortTreeMutator>>(),
            _state,
            _registry);

        // Wire a real ClusterBuilder and MQTT ruleset so the mutator's happy paths can run end-to-end.
        _clusterBuilder = BuilderFactory.Create();
        _datastore.Builder.Returns(_clusterBuilder);
        _datastore.ActiveDataflow.Returns(_clusterBuilder.Cluster.Dataflows[0]);
        _rulesetProvider.GetRuleset(Arg.Any<RulesetIdentifier>()).Returns(TestResources.MqttRuleset);
        _rulesetProvider
            .GetSystemDataPortDependency()
            .Returns(new ClusterDependency { Name = "SystemDataPort", Version = "1.0.0" });
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        _clusterBuilder.Dispose();
        await _datastore.DisposeAsync();
    }

    private DataPortRootNodeModel CreateRootWithBroker()
    {
        _mutator.CreateNewDataPortRootNode(MqttRootId);
        return _state.RootNodes.Single();
    }

    [Fact]
    public void CreateNewChildNode_WhenParentIsNotDataPortNode_DoesNothing()
    {
        // Arrange
        var parent = Substitute.For<ITreeNode>();
        var child = Substitute.For<ITreeNode>();

        // Act
        _mutator.CreateNewChildNode(parent, child);

        // Assert
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyChildrenChanged((ITreeNode)default!);
    }

    [Fact]
    public void CreateNewChildNode_WhenDatastoreBuilderIsNull_DoesNothing()
    {
        // Arrange
        _datastore.Builder.Returns((IClusterBuilder?)null);
        var parent = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(rootNode: parent);

        // Act
        _mutator.CreateNewChildNode(parent, child);

        // Assert
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyChildrenChanged((ITreeNode)default!);
    }

    [Fact]
    public void CreateNewChildNode_AddsTreeNodeToExistingDataPort()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folderDescriptor = broker.PossibleChildren[0];
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(folderDescriptor, broker);

        var dataPortCountBefore = _clusterBuilder.Cache.DataPorts.Count;

        // Act
        _mutator.CreateNewChildNode(broker, folder);

        // Assert
        Assert.Contains(folder, broker.Children);
        Assert.Equal(dataPortCountBefore, _clusterBuilder.Cache.DataPorts.Count);
        Assert.NotEmpty(_clusterBuilder.Cache.DataPortTreeNodes);
    }

    [Fact]
    public void CreateNewDataPortRootNode_CreatesRootNodeWithFirstChildAndDataPort()
    {
        // Arrange - a previously selected node that should be deselected afterwards
        var previouslySelected = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        previouslySelected.Selected = true;
        _state.AddRootNode(previouslySelected);

        // Act
        _mutator.CreateNewDataPortRootNode(MqttRootId);
        var root = _state.GetDataPortRootNode(MqttRootId)!;

        // Assert
        Assert.Equal(MqttRootId, root.Builder.Ruleset.Root?.Id);
        Assert.Single(root.Children);
        Assert.Single(_clusterBuilder.Cache.DataPorts);
        _builder.Notifications.ReceivedWithAnyArgs().NotifyChildrenChanged((ITreeNode)default!);
        _builder.Selection.Received().ChangeSelection(previouslySelected, false);
    }

    [Fact]
    public void DeleteDataPortTreeNode_WhenDatastoreBuilderIsNull_DoesNothing()
    {
        // Arrange
        _datastore.Builder.Returns((IClusterBuilder?)null);
        var node = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        var args = new VisibleActionArguments { Builder = _builder, Node = node };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        Assert.False(_state.IsDeletionInProgress);
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForRootNode_RemovesRootAndDataPorts()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var args = new VisibleActionArguments { Builder = _builder, Node = root };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        Assert.Empty(_state.RootNodes);
        Assert.Empty(_clusterBuilder.Cache.DataPorts);
        Assert.True(_state.IsDeletionInProgress);
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForRootNode_RemovesSystemDataPortDependency()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var args = new VisibleActionArguments { Builder = _builder, Node = root };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        Assert.DoesNotContain(_clusterBuilder.Cache.ClusterDependencies, d => d.Name == "SystemDataPort");
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForDataPortChild_RemovesDataPort()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var args = new VisibleActionArguments { Builder = _builder, Node = broker };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        Assert.Empty(_clusterBuilder.Cache.DataPorts);
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForChildOfChild_RemovesDataPortTreeNode()
    {
        // Arrange - add a folder (DataPortTreeNode) beneath the broker (child of child)
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folderDescriptor = broker.PossibleChildren[0];
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(folderDescriptor, broker);
        _mutator.CreateNewChildNode(broker, folder);

        Assert.NotEmpty(_clusterBuilder.Cache.DataPortTreeNodes);

        var args = new VisibleActionArguments { Builder = _builder, Node = folder };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert - only the tree node is removed, the data port itself remains
        Assert.Empty(_clusterBuilder.Cache.DataPortTreeNodes);
        Assert.Single(_clusterBuilder.Cache.DataPorts);
    }

    [Fact]
    public void DeleteDataPortTreeNode_WhenConfirmationRequested_DefersDeletion()
    {
        // Arrange
        var root = CreateRootWithBroker();
        Action? confirm = null;
        _state.OnDeleteNodeUserConfirmationRequest = (_, action) => confirm = action;
        var args = new VisibleActionArguments { Builder = _builder, Node = root };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert - not deleted until confirmation callback runs
        Assert.Single(_state.RootNodes);
        Assert.NotNull(confirm);

        confirm!();
        Assert.Empty(_state.RootNodes);
    }

    [Fact]
    public void InitializeDataPortTree_RebuildsRootNodesFromCache()
    {
        // Arrange - create a data port and register its tree builder
        CreateRootWithBroker();

        // Act
        _mutator.InitializeDataPortTree();

        // Assert
        var item = Assert.Single(_state.RootNodes);
        Assert.Single(item.Children);
    }

    [Fact]
    public void InitializeDataPortTree_WhenNoTreeBuilderForDataPort_SwallowsExceptionAndSkipsRoot()
    {
        // Arrange - a data port exists in the cache but the registry has no matching tree builder
        CreateRootWithBroker();
        _registry.Initialize(); // clears registered tree builders (no ruleset identifiers configured)

        // Act - TryCreateDataPortTree throws internally and the exception is caught/logged
        _mutator.InitializeDataPortTree();

        // Assert - the failing data port produced no root node
        Assert.Empty(_state.RootNodes);
    }

    [Fact]
    public void ProcessNodeChanges_WhenNodeIsNotChildNode_DoesNothing()
    {
        // Arrange
        var node = DataPortNodeModelCreator.CreateDataPortRootNodeModel();

        // Act & Assert (should not throw)
        _mutator.ProcessNodeChanges(node);
    }

    [Fact]
    public void ProcessNodeChanges_WhenDatastoreBuilderIsNull_DoesNothing()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        _datastore.Builder.Returns((IClusterBuilder?)null);

        // Act & Assert (should not throw and leaves the node untouched)
        _mutator.ProcessNodeChanges(broker);
    }

    [Fact]
    public void ProcessNodeChanges_ForTreeNodeChild_WhenClusterNodeNotFound_DoesNothing()
    {
        // Arrange - a folder child that is not present in the cluster builder cache
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        folder.Name = "Renamed Folder";

        // Act & Assert (should not throw; folder is not in cache so nothing is updated)
        _mutator.ProcessNodeChanges(folder);
        Assert.Equal("Renamed Folder", folder.Name);
    }

    [Fact]
    public void ProcessNodeChanges_ForDataPortChild_UpdatesDataPortName()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        broker.Name = "Renamed Broker";

        // Act
        _mutator.ProcessNodeChanges(broker);

        // Assert
        var dataPort = _clusterBuilder.Cache.DataPorts.Single();
        Assert.Equal("Renamed Broker", dataPort.Name);
        Assert.Equal(dataPort.Name, broker.Name);
    }

    [Fact]
    public void ProcessNodeChanges_ForDataPortChild_WhenNameCollides_SyncsBuilderAdjustedName()
    {
        // Arrange - two brokers so that a duplicate name forces the builder to adjust it
        var root = CreateRootWithBroker();
        var firstBroker = (DataPortChildNodeModel)root.Children.Single();

        var secondBrokerDescriptor = root.PossibleChildren[0];
        var secondBroker = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(secondBrokerDescriptor, root);
        _mutator.CreateNewChildNode(root, secondBroker);

        // Rename the second broker to the first broker's name -> builder appends a suffix
        secondBroker.Name = firstBroker.Name;

        // Act
        _mutator.ProcessNodeChanges(secondBroker);

        // Assert - the node name was synced back to the builder-adjusted (unique) name
        var adjustedDataPort = _clusterBuilder.Cache.DataPorts.Single(d => d.Id == secondBroker.Id.Value);
        Assert.NotEqual(firstBroker.Name, secondBroker.Name);
        Assert.Equal(adjustedDataPort.Name, secondBroker.Name);
    }

    [Fact]
    public void ProcessNodeChanges_ForTreeNodeChild_UpdatesDataPortTreeNode()
    {
        // Arrange - add a folder (DataPortTreeNode) beneath the broker
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folderDescriptor = broker.PossibleChildren[0];
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(folderDescriptor, broker);
        _mutator.CreateNewChildNode(broker, folder);

        folder.Name = "Renamed Folder";

        // Act
        _mutator.ProcessNodeChanges(folder);

        // Assert
        var treeNode = _clusterBuilder.Cache.DataPortTreeNodes.Single(k => k.Id == folder.Id.Value);
        Assert.Equal("Renamed Folder", treeNode.Name);
        Assert.Equal(treeNode.Name, folder.Name);
    }

    [Fact]
    public void ProcessNodeChanges_ForTreeNodeChild_WhenNameCollides_SyncsBuilderAdjustedName()
    {
        // Arrange - two folders beneath the broker so a duplicate name forces the builder to adjust it
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();

        var firstFolder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, firstFolder);

        var secondFolder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, secondFolder);

        // Rename the second folder to the first folder's name -> builder appends a suffix
        secondFolder.Name = firstFolder.Name;

        // Act
        _mutator.ProcessNodeChanges(secondFolder);

        // Assert - the node name was synced back to the builder-adjusted (unique) name
        var adjustedTreeNode = _clusterBuilder.Cache.DataPortTreeNodes.Single(k => k.Id == secondFolder.Id.Value);
        Assert.NotEqual(firstFolder.Name, secondFolder.Name);
        Assert.Equal(adjustedTreeNode.Name, secondFolder.Name);
    }

    [Fact]
    public void RevertNodeChanges_ForDataPortChild_RestoresValuesFromDataPort()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        var originalName = broker.Name;
        broker.Name = "Unsaved Name";

        // Act
        _mutator.RevertNodeChanges(broker);

        // Assert
        Assert.Equal(originalName, broker.Name);
    }

    [Fact]
    public void RevertNodeChanges_ForTreeNodeChild_RestoresValuesFromTreeNode()
    {
        // Arrange - add a folder (DataPortTreeNode) beneath the broker
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, folder);

        var originalName = folder.Name;
        folder.Name = "Unsaved Folder Name";

        // Act
        _mutator.RevertNodeChanges(folder);

        // Assert
        Assert.Equal(originalName, folder.Name);
    }

    [Fact]
    public void RevertNodeChanges_WhenDatastoreBuilderIsNull_DoesNothing()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        broker.Name = "Unsaved Name";
        _datastore.Builder.Returns((IClusterBuilder?)null);

        // Act & Assert (should not throw and leaves the node untouched)
        _mutator.RevertNodeChanges(broker);
        Assert.Equal("Unsaved Name", broker.Name);
    }
}
