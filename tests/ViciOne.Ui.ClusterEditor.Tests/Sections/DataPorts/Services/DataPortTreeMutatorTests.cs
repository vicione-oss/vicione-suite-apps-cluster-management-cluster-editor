using System;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components.Localization;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
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
    private const string FloatDataPointNodeTypeId = "DataPointFloat";
    private const string FolderNodeTypeId = "Folder";
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
        _datastore.HasBuilder.Returns(true);
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

    // The demo elements of BuilderFactory contain no function blocks, so a block of the test design provides the connectors.
    private void AssignConnector(DataPortTreeNode treeNode)
    {
        _clusterBuilder.Editors.Container.AddFunctionBlock(_clusterBuilder.Cluster.Dataflows[0].Root, BuilderFactory.FbDesignId);

        var connector = _clusterBuilder.Cache.Connectors.First(c =>
            c.Type == ConnectorType.ProcessData && _clusterBuilder.Editors.DataPortTreeNode.CanAssignConnector(treeNode, c));
        _clusterBuilder.Editors.DataPortTreeNode.AssignConnector(treeNode, connector);

        Assert.NotEmpty(treeNode.Links);
    }

    // Creates Broker -> Folder -> DataPoint Float in the tree and in the cluster.
    private (DataPortChildNodeModel Broker, DataPortChildNodeModel Value, DataPortTreeNode ValueTreeNode) CreateBrokerWithValue()
    {
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();

        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            broker.PossibleChildren.First(d => d.NodeReference.Id == FolderNodeTypeId), broker);
        _mutator.CreateNewChildNode(broker, folder);

        var value = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(
            folder.PossibleChildren.First(d => d.NodeReference.Id == FloatDataPointNodeTypeId), folder);
        _mutator.CreateNewChildNode(folder, value);

        return (broker, value, _clusterBuilder.Cache.DataPortTreeNodeIds[value.Id.Value]);
    }

    private DataPortRootNodeModel CreateRootWithBroker()
    {
        _mutator.CreateNewDataPortRootNode(MqttRootId);
        return _state.RootNodes.Single();
    }

    private static string GetValueTypeName(DataPortChildNodeModel value, DataPortTreeNode valueTreeNode, Func<Type, bool> predicate)
        => value.GetRequiredSystemProperty<string>(nameof(DataPortTreeNode.ValueType)).AvailableValues
            .First(name => value.RootNode.Builder.DataTypes[name].RuntimeType is { } runtimeType
                && runtimeType != valueTreeNode.ValueType
                && predicate(runtimeType));

    [Fact]
    public void CanApplyNodeChanges_WhenDataPortNoLongerInCluster_ReturnsTrue()
    {
        // Arrange - not validated here by design; committing changes of a removed DataPort is a separate issue
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root, dataPortDirection: DataPortDirection.Out);
        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPort.Direction), DataPortDirection.In);

        // Act
        var result = _mutator.CanApplyNodeChanges(node, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenDirectionChangeAllowedWithoutLinks_ReturnsTrue()
    {
        // Arrange
        var (broker, _, _) = CreateBrokerWithValue();
        var dataPort = _clusterBuilder.Cache.DataPortIds[broker.Id.Value];
        var direction = Enum.GetValues<DataPortDirection>()
            .First(d => d != dataPort.Direction && _clusterBuilder.Editors.DataPort.CanSetDirection(dataPort, d));

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPort.Direction), direction);

        // Act
        var result = _mutator.CanApplyNodeChanges(broker, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenDirectionConflictsWithLinkOnDescendant_ReturnsFalseWithMessage()
    {
        // Arrange - regression #1700: a link is added below the DataPort while its edit form is open
        var (broker, _, valueTreeNode) = CreateBrokerWithValue();
        var dataPort = _clusterBuilder.Cache.DataPortIds[broker.Id.Value];
        var allowedDirectionsBeforeLink = Enum.GetValues<DataPortDirection>()
            .Where(d => d != dataPort.Direction && _clusterBuilder.Editors.DataPort.CanSetDirection(dataPort, d))
            .ToList();

        AssignConnector(valueTreeNode);

        var direction = allowedDirectionsBeforeLink.First(d => !_clusterBuilder.Editors.DataPort.CanSetDirection(dataPort, d));
        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPort.Direction), direction);

        // Act
        var result = _mutator.CanApplyNodeChanges(broker, pendingValues, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal(DataPortSection.DirectionChangeNotPossible, errorMessage);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenDirectionUnchanged_ReturnsTrue()
    {
        // Arrange - the builder rejects every direction, so only the unchanged check can lead to true
        var (broker, _, _) = CreateBrokerWithValue();
        var dataPort = _clusterBuilder.Cache.DataPortIds[broker.Id.Value];

        var clusterBuilder = Substitute.For<IClusterBuilder>();
        clusterBuilder.Cache.DataPortIds.Returns(_clusterBuilder.Cache.DataPortIds);
        clusterBuilder.Editors.DataPort.CanSetDirection(Arg.Any<DataPort>(), Arg.Any<DataPortDirection>()).Returns(false);
        _datastore.Builder.Returns(clusterBuilder);

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPort.Direction), dataPort.Direction);

        // Act
        var result = _mutator.CanApplyNodeChanges(broker, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
        clusterBuilder.Editors.DataPort.DidNotReceiveWithAnyArgs().CanSetDirection(default!, default);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenNoBuilderLoaded_ReturnsTrue()
    {
        // Arrange
        _datastore.HasBuilder.Returns(false);
        var root = DataPortNodeModelCreator.CreateDataPortRootNodeModel();
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root, dataPortDirection: DataPortDirection.Out);
        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPort.Direction), DataPortDirection.In);

        // Act
        var result = _mutator.CanApplyNodeChanges(node, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenValueTypeChangeAllowed_ReturnsTrue()
    {
        // Arrange
        var (_, value, valueTreeNode) = CreateBrokerWithValue();
        var valueTypeName = GetValueTypeName(value, valueTreeNode,
            runtimeType => _clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(valueTreeNode, runtimeType));

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPortTreeNode.ValueType), valueTypeName);

        // Act
        var result = _mutator.CanApplyNodeChanges(value, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenValueTypeConflictsWithLink_ReturnsFalseWithMessage()
    {
        // Arrange - the builder only rejects value types that are incompatible with the linked connectors. The test
        // resources provide no such link for any data point, so the rejection of the builder is stubbed here.
        var (_, value, valueTreeNode) = CreateBrokerWithValue();
        var valueTypeName = GetValueTypeName(value, valueTreeNode, _ => true);
        var valueType = value.RootNode.Builder.DataTypes[valueTypeName].RuntimeType;

        var clusterBuilder = Substitute.For<IClusterBuilder>();
        clusterBuilder.Cache.DataPortTreeNodeIds.Returns(_clusterBuilder.Cache.DataPortTreeNodeIds);
        clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(Arg.Any<DataPortTreeNode>(), Arg.Any<Type>()).Returns(true);
        clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(valueTreeNode, valueType).Returns(false);
        _datastore.Builder.Returns(clusterBuilder);

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPortTreeNode.ValueType), valueTypeName);

        // Act
        var result = _mutator.CanApplyNodeChanges(value, pendingValues, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.Equal(DataPortSection.ValueTypeChangeNotPossible, errorMessage);
        clusterBuilder.Editors.DataPortTreeNode.Received(1).CanSetValueType(valueTreeNode, valueType);
    }

    [Fact]
    public void CanApplyNodeChanges_WhenValueTypeUnchanged_ReturnsTrue()
    {
        // Arrange - the builder rejects every value type, so only the unchanged check can lead to true
        var (_, value, valueTreeNode) = CreateBrokerWithValue();
        var valueTypeName = value.GetRequiredSystemProperty<string>(nameof(DataPortTreeNode.ValueType)).TypedValue;
        Assert.Equal(valueTreeNode.ValueType, value.RootNode.Builder.DataTypes[valueTypeName!].RuntimeType);

        var clusterBuilder = Substitute.For<IClusterBuilder>();
        clusterBuilder.Cache.DataPortTreeNodeIds.Returns(_clusterBuilder.Cache.DataPortTreeNodeIds);
        clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(Arg.Any<DataPortTreeNode>(), Arg.Any<Type>()).Returns(false);
        _datastore.Builder.Returns(clusterBuilder);

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPortTreeNode.ValueType), valueTypeName);

        // Act
        var result = _mutator.CanApplyNodeChanges(value, pendingValues, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
        clusterBuilder.Editors.DataPortTreeNode.DidNotReceiveWithAnyArgs().CanSetValueType(default!, default!);
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
        broker.Children.Should().Contain(folder);
        _clusterBuilder.Cache.DataPorts.Count.Should().Be(dataPortCountBefore);
        _clusterBuilder.Cache.DataPortTreeNodes.Should().NotBeEmpty();
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
        root.Builder.Ruleset.Root?.Id.Should().Be(MqttRootId);
        root.Children.Should().ContainSingle();
        _clusterBuilder.Cache.DataPorts.Should().ContainSingle();
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
        _state.IsDeletionInProgress.Should().BeFalse();
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
        _state.RootNodes.Should().BeEmpty();
        _clusterBuilder.Cache.DataPorts.Should().BeEmpty();
        _state.IsDeletionInProgress.Should().BeTrue();
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
        _clusterBuilder.Cache.ClusterDependencies.Should().NotContain(d => d.Name == "SystemDataPort");
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
        _clusterBuilder.Cache.DataPorts.Should().BeEmpty();
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

        _clusterBuilder.Cache.DataPortTreeNodes.Should().NotBeEmpty();

        var args = new VisibleActionArguments { Builder = _builder, Node = folder };

        // Act
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert - only the tree node is removed, the data port itself remains
        _clusterBuilder.Cache.DataPortTreeNodes.Should().BeEmpty();
        _clusterBuilder.Cache.DataPorts.Should().ContainSingle();
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
        _state.RootNodes.Should().ContainSingle();
        confirm.Should().NotBeNull();

        confirm!();
        _state.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForChildOfChild_WhenAlreadyDeleted_DoesNothing()
    {
        // Arrange - the tree still shows the folder after the first deletion, so it can be deleted again
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, folder);

        var args = new VisibleActionArguments { Builder = _builder, Node = folder };
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Act
        var act = () => _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        act.Should().NotThrow();
        _clusterBuilder.Cache.DataPortTreeNodes.Should().BeEmpty();
        _clusterBuilder.Cache.DataPorts.Should().ContainSingle();
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForChildOfDeletedParent_DoesNothing()
    {
        // Arrange - deleting the folder takes its child with it, but the tree still shows the child
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, folder);
        var child = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(folder.PossibleChildren[0], folder);
        _mutator.CreateNewChildNode(folder, child);

        _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = folder });

        // Act
        var act = () => _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = child });

        // Assert
        act.Should().NotThrow();
        _clusterBuilder.Cache.DataPortTreeNodes.Should().BeEmpty();
        _clusterBuilder.Cache.DataPorts.Should().ContainSingle();
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForDataPortChild_WhenAlreadyDeleted_DoesNothing()
    {
        // Arrange
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var args = new VisibleActionArguments { Builder = _builder, Node = broker };
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Act
        var act = () => _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        act.Should().NotThrow();
        _clusterBuilder.Cache.DataPorts.Should().BeEmpty();
    }

    [Fact]
    public void DeleteDataPortTreeNode_ForRootNode_WhenDataPortsAlreadyRemoved_DoesNothing()
    {
        // Arrange - the root node still lists the broker that was deleted on its own
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = broker });

        // Act
        var act = () => _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = root });

        // Assert
        act.Should().NotThrow();
        _state.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void DeleteDataPortTreeNode_WhenNothingWasDeleted_KeepsDeletionInProgressUnset()
    {
        // Arrange - the flag is reset by the builder events of the first deletion
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var args = new VisibleActionArguments { Builder = _builder, Node = broker };
        _mutator.DeleteDataPortTreeNode(default!, args);
        _state.IsDeletionInProgress = false;

        // Act - a deletion the builder never sees raises no event that could reset the flag again
        _mutator.DeleteDataPortTreeNode(default!, args);

        // Assert
        _state.IsDeletionInProgress.Should().BeFalse();
    }

    [Fact]
    public void InitializeDataPortTree_RebuildsRootNodesFromCache()
    {
        // Arrange - create a data port and register its tree builder
        CreateRootWithBroker();

        // Act
        _mutator.InitializeDataPortTree();

        // Assert
        var rootNode = _state.RootNodes.Should().ContainSingle().Which;
        rootNode.Children.Should().ContainSingle();
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
        _state.RootNodes.Should().BeEmpty();
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
        folder.Name.Should().Be("Renamed Folder");
    }

    [Fact]
    public void ProcessNodeChanges_ForDataPortChild_WhenDataPortWasRemoved_DoesNothing()
    {
        // Arrange - the broker is still in edit mode while its data port is already deleted
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = broker });
        broker.Name = "Renamed Broker";

        // Act
        var act = () => _mutator.ProcessNodeChanges(broker);

        // Assert
        act.Should().NotThrow();
        broker.Name.Should().Be("Renamed Broker");
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
        dataPort.Name.Should().Be("Renamed Broker");
        broker.Name.Should().Be(dataPort.Name);
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
        secondBroker.Name.Should().NotBe(firstBroker.Name);
        secondBroker.Name.Should().Be(adjustedDataPort.Name);
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
        treeNode.Name.Should().Be("Renamed Folder");
        folder.Name.Should().Be(treeNode.Name);
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
        secondFolder.Name.Should().NotBe(firstFolder.Name);
        secondFolder.Name.Should().Be(adjustedTreeNode.Name);
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
        broker.Name.Should().Be(originalName);
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
        folder.Name.Should().Be(originalName);
    }

    [Fact]
    public void RevertNodeChanges_ForDataPortChild_WhenDataPortWasRemoved_DoesNothing()
    {
        // Arrange - the broker is still in edit mode while its data port is already deleted
        var root = CreateRootWithBroker();
        var broker = (DataPortChildNodeModel)root.Children.Single();
        _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = broker });
        broker.Name = "Unsaved Name";

        // Act
        var act = () => _mutator.RevertNodeChanges(broker);

        // Assert - nothing is left to restore the values from
        act.Should().NotThrow();
        broker.Name.Should().Be("Unsaved Name");
    }

    [Fact]
    public void RevertNodeChanges_ForTreeNodeChild_WhenTreeNodeWasRemoved_DoesNothing()
    {
        // Arrange - the folder is still in edit mode while its tree node is already deleted
        var root = CreateRootWithBroker();
        var broker = root.Children.Single();
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(broker.PossibleChildren[0], broker);
        _mutator.CreateNewChildNode(broker, folder);
        _mutator.DeleteDataPortTreeNode(default!, new VisibleActionArguments { Builder = _builder, Node = folder });
        folder.Name = "Unsaved Folder Name";

        // Act
        var act = () => _mutator.RevertNodeChanges(folder);

        // Assert - nothing is left to restore the values from
        act.Should().NotThrow();
        folder.Name.Should().Be("Unsaved Folder Name");
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
        broker.Name.Should().Be("Unsaved Name");
    }
}
