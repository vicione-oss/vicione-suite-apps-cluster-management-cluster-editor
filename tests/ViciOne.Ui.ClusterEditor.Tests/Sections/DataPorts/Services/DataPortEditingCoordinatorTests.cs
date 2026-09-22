using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components.Localization;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortEditingCoordinatorTests : IAsyncDisposable
{
    private const string PendingName = "Renamed";

    private readonly ITreeBuilder _builder;
    private readonly DataPortEditingCoordinator _coordinator;
    private readonly IClusterEditorManagementInternal _dataManagementService;
    private readonly IDatastore _datastore;
    private readonly DataPortChildNodePropertyValueStore _propertyValueStore = new();
    private readonly DataPortTreeState _state;

    public DataPortEditingCoordinatorTests()
    {
        _dataManagementService = Substitute.For<IClusterEditorManagementInternal>();
        _builder = Substitute.For<ITreeBuilder>();
        _state = new DataPortTreeState { Builder = _builder };

        var rulesetProvider = Substitute.For<IRulesetProvider>();
        _datastore = Substitute.For<IDatastore>();
        var mutator = new DataPortTreeMutator(
            _datastore,
            rulesetProvider,
            Substitute.For<ILogger<DataPortTreeMutator>>(),
            _state,
            new DataPortTreeBuilderRegistry(rulesetProvider, new FakeLogger<DataPortTreeBuilderRegistry>()));

        _coordinator = new DataPortEditingCoordinator(_dataManagementService, mutator, _propertyValueStore, _state);
    }

    public async ValueTask DisposeAsync()
    {
        _builder.Dispose();
        await _datastore.DisposeAsync();
    }

    private static DataPortRootNodeModel CreateRoot()
        => new()
        {
            Builder = new Tree.Builder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };

    private static DataPortChildNodeModel CreateChild(DataPortRootNodeModel root)
        => new()
        {
            LinkDirections = [],
            Name = "Child",
            Parent = root,
            Properties = [],
            RootNode = root,
            TransferDirections = [],
        };

    // Creates a DataPort node in edit mode whose pending values change the name and the direction (Out -> In).
    private (DataPortChildNodeModel Node, DataPortChildNodePropertyValueStore PendingValues) CreateDataPortNodeWithPendingDirectionChange(bool canSetDirection)
    {
        var root = CreateRoot();
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root, dataPortDirection: DataPortDirection.Out);
        node.IsEditModeActive = true;

        var dataPort = new DataPort { Direction = DataPortDirection.Out, Id = node.Id.Value, Name = node.Name };
        _datastore.HasBuilder.Returns(true);
        _datastore.Builder.Cache.DataPortIds.Returns(new Dictionary<Guid, DataPort> { [dataPort.Id] = dataPort });
        // Only the exact arguments decide, so a wrong DataPort or direction cannot lead to the expected result.
        _datastore.Builder.Editors.DataPort.CanSetDirection(Arg.Any<DataPort>(), Arg.Any<DataPortDirection>()).Returns(!canSetDirection);
        _datastore.Builder.Editors.DataPort.CanSetDirection(dataPort, DataPortDirection.In).Returns(canSetDirection);

        var pendingValues = new DataPortChildNodePropertyValueStore();
        pendingValues.Set(nameof(DataPortChildNodeModel.Name), PendingName);
        pendingValues.Set(nameof(DataPort.Direction), DataPortDirection.In);

        return (node, pendingValues);
    }

    [Fact]
    public async Task BeginEdit_WhenNoNodeBeingEdited_ActivatesEditModeAndNotifies()
    {
        // Arrange
        var root = CreateRoot();
        var node = CreateChild(root);

        // Act
        await _coordinator.BeginEdit(node);

        // Assert
        Assert.True(node.IsEditModeActive);
        Assert.False(node.HasChangedProperties);
        Assert.Same(node, _state.EditingTreeNode);
        _builder.Notifications.Received().NotifyNodeChanged(node, ChangedNodeDetail.None);
    }

    [Fact]
    public async Task BeginEdit_WhenNoNodeBeingEdited_NotifiesDescendants()
    {
        // Arrange - already rendered descendants must be rendered again to be locked for dragging
        var root = CreateRoot();
        var node = CreateChild(root);
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: node, rootNode: root);
        node.Children.Add(child);

        // Act
        await _coordinator.BeginEdit(node);

        // Assert
        _builder.Notifications.Received(1).NotifyNodeChanged(child, ChangedNodeDetail.None);
    }

    [Fact]
    public async Task BeginEdit_WhenEditingNodeHasUnsavedChanges_ShowsToastAndDoesNotSwitch()
    {
        // Arrange
        var root = CreateRoot();
        var editingNode = CreateChild(root);
        editingNode.IsEditModeActive = true;
        editingNode.HasChangedProperties = true;
        _state.EditingTreeNode = editingNode;

        var newNode = CreateChild(root);

        // Act
        await _coordinator.BeginEdit(newNode);

        // Assert
        await _dataManagementService.Received(1).ShowMessageToast(LogLevel.Warning, Arg.Any<string>(), Arg.Any<Action>());
        Assert.False(newNode.IsEditModeActive);
        Assert.Same(editingNode, _state.EditingTreeNode);
    }

    [Fact]
    public async Task BeginEdit_WhenTheEditedNodeIsRemovedWhileTheToastIsOpen_ScrollBackDoesNothing()
    {
        // Arrange - the toast's callback runs long after it was handed over, by which time the
        // node it points at may have gone with a deleted ancestor.
        var root = CreateRoot();
        var editingNode = CreateChild(root);
        editingNode.IsEditModeActive = true;
        editingNode.HasChangedProperties = true;
        _state.EditingTreeNode = editingNode;

        Action? scrollBack = null;
        _dataManagementService
            .ShowMessageToast(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Do<Action>(callback => scrollBack = callback))
            .Returns(Task.CompletedTask);

        await _coordinator.BeginEdit(CreateChild(root));
        Assert.NotNull(scrollBack);
        _state.EditingTreeNode = null;

        // Act
        scrollBack();

        // Assert
        _builder.Expansion.DidNotReceiveWithAnyArgs().ExpandToNode(default!);
        _builder.Scrolling.DidNotReceiveWithAnyArgs().RequestScrollToNode(default!);
    }

    [Fact]
    public async Task BeginEdit_WhenEditingNodeHasNoChanges_DeactivatesPreviousAndActivatesNew()
    {
        // Arrange
        var root = CreateRoot();
        var editingNode = CreateChild(root);
        editingNode.IsEditModeActive = true;
        editingNode.HasChangedProperties = false;
        _state.EditingTreeNode = editingNode;

        var newNode = CreateChild(root);

        // Act
        await _coordinator.BeginEdit(newNode);

        // Assert
        Assert.False(editingNode.IsEditModeActive);
        Assert.True(newNode.IsEditModeActive);
        Assert.Same(newNode, _state.EditingTreeNode);
        _builder.Notifications.Received().NotifyNodeChanged(editingNode, ChangedNodeDetail.None);
        _builder.Notifications.Received().NotifyNodeChanged(newNode, ChangedNodeDetail.None);
    }

    [Fact]
    public async Task BeginEdit_WhenEditingNodeHasNoChanges_NotifiesDescendantsOfPreviousNode()
    {
        // Arrange - descendants of the previous node were locked and must be rendered draggable again
        var root = CreateRoot();
        var editingNode = CreateChild(root);
        editingNode.IsEditModeActive = true;
        var editingNodeChild = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: editingNode, rootNode: root);
        editingNode.Children.Add(editingNodeChild);
        _state.EditingTreeNode = editingNode;

        var newNode = CreateChild(root);

        // Act
        await _coordinator.BeginEdit(newNode);

        // Assert
        _builder.Notifications.Received(1).NotifyNodeChanged(editingNodeChild, ChangedNodeDetail.None);
    }

    [Fact]
    public async Task BeginEdit_WhenANewEditStarts_DropsPendingValuesOfAnEarlierEdit()
    {
        // Arrange - a restored edit was cancelled, its pending values must not come back
        var root = CreateRoot();
        var node = CreateChild(root);
        _state.AddRootNode(root);
        root.Children.Add(node);
        await _coordinator.RestorePendingEditAsync(new DataPortPendingEdit(node.Id, new Dictionary<string, object?> { [nameof(node.Name)] = PendingName }));
        node.IsEditModeActive = false;

        // Act
        await _coordinator.BeginEdit(node);

        // Assert
        Assert.Empty(_coordinator.GetPendingValues(node));
    }

    [Fact]
    public void CapturePendingEdit_WhenNoNodeIsBeingEdited_ReturnsNull()
    {
        // Act
        var pendingEdit = _coordinator.CapturePendingEdit();

        // Assert
        Assert.Null(pendingEdit);
    }

    [Fact]
    public void CapturePendingEdit_WhenTheEditFormHasNoChanges_ReturnsTheNodeWithoutChangedValues()
    {
        // Arrange
        var node = CreateChild(CreateRoot());
        node.IsEditModeActive = true;
        _state.EditingTreeNode = node;
        _propertyValueStore.Set(nameof(node.Name), PendingName);

        // Act
        var pendingEdit = _coordinator.CapturePendingEdit();

        // Assert
        Assert.NotNull(pendingEdit);
        Assert.Equal(node.Id, pendingEdit.NodeId);
        Assert.Empty(pendingEdit.ChangedValues);
    }

    [Fact]
    public void CapturePendingEdit_WhenTheEditFormHasChanges_ReturnsOnlyTheChangedValues()
    {
        // Arrange
        var root = CreateRoot();
        var node = DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root, dataPortDirection: DataPortDirection.Out);
        node.IsEditModeActive = true;
        node.HasChangedProperties = true;
        _state.EditingTreeNode = node;
        _propertyValueStore.Set(nameof(node.Name), node.Name);
        _propertyValueStore.Set(nameof(DataPort.Direction), DataPortDirection.In);
        _propertyValueStore.Set("NotAPropertyOfTheNode", 42);

        // Act
        var pendingEdit = _coordinator.CapturePendingEdit();

        // Assert
        Assert.NotNull(pendingEdit);
        var changedValue = Assert.Single(pendingEdit.ChangedValues);
        Assert.Equal(nameof(DataPort.Direction), changedValue.Key);
        Assert.Equal(DataPortDirection.In, changedValue.Value);
    }

    [Fact]
    public void GetPendingValues_WhenAnotherNodeIsBeingEdited_ReturnsNoValues()
    {
        // Arrange
        var root = CreateRoot();
        _state.EditingTreeNode = CreateChild(root);
        _state.PendingEditValues = new Dictionary<string, object?> { [nameof(DataPortChildNodeModel.Name)] = PendingName };

        // Act
        var pendingValues = _coordinator.GetPendingValues(CreateChild(root));

        // Assert
        Assert.Empty(pendingValues);
    }

    [Fact]
    public async Task RestorePendingEditAsync_WhenTheNodeIsGoneAndHadChanges_ShowsWarningToast()
    {
        // Arrange
        var node = CreateChild(CreateRoot());
        var pendingEdit = new DataPortPendingEdit(node.Id, new Dictionary<string, object?> { [nameof(node.Name)] = PendingName });

        // Act
        await _coordinator.RestorePendingEditAsync(pendingEdit);

        // Assert
        await _dataManagementService.Received(1).ShowMessageToast(LogLevel.Warning, DataPortSection.PendingChangesDiscarded, Arg.Any<Action>());
        Assert.Null(_state.EditingTreeNode);
    }

    [Fact]
    public async Task RestorePendingEditAsync_WhenTheNodeIsGoneWithoutChanges_ShowsNoToast()
    {
        // Arrange
        var node = CreateChild(CreateRoot());

        // Act
        await _coordinator.RestorePendingEditAsync(new DataPortPendingEdit(node.Id, new Dictionary<string, object?>()));

        // Assert
        await _dataManagementService.DidNotReceiveWithAnyArgs().ShowMessageToast(default, default!, default!);
    }

    [Fact]
    public async Task RestorePendingEditAsync_WhenTheNodeStillExists_ReopensTheEditFormWithTheChangedValues()
    {
        // Arrange
        var root = CreateRoot();
        var node = CreateChild(root);
        _state.AddRootNode(root);
        root.Children.Add(node);
        var changedValues = new Dictionary<string, object?> { [nameof(node.Name)] = PendingName };

        // Act
        await _coordinator.RestorePendingEditAsync(new DataPortPendingEdit(node.Id, changedValues));

        // Assert
        Assert.True(node.IsEditModeActive);
        Assert.True(node.HasChangedProperties);
        Assert.Same(node, _state.EditingTreeNode);
        Assert.Equal(changedValues, _coordinator.GetPendingValues(node));
        _builder.Notifications.Received(1).NotifyNodeChanged(node, ChangedNodeDetail.None);
        _builder.Scrolling.Received(1).RequestScrollToNode(node);
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenChangesInvalid_DoesNotAssignPendingValuesToModel()
    {
        // Arrange
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: false);
        var originalName = node.Name;

        // Act
        await _coordinator.TryConfirmEditAsync(node, pendingValues);

        // Assert
        Assert.Equal(originalName, node.Name);
        Assert.Equal(DataPortDirection.Out, node.GetRequiredSystemProperty<DataPortDirection>().TypedValue);
        Assert.True(node.IsEditModeActive);
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenChangesInvalid_ShowsWarningToastAndReturnsFalse()
    {
        // Arrange
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: false);

        // Act
        var result = await _coordinator.TryConfirmEditAsync(node, pendingValues);

        // Assert
        Assert.False(result);
        await _dataManagementService.Received(1).ShowMessageToast(LogLevel.Warning, DataPortSection.DirectionChangeNotPossible, Arg.Any<Action>());
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenTheNodeIsRemovedWhileTheToastIsOpen_ScrollToNodeDoesNothing()
    {
        // Arrange - the toast's callback runs long after it was handed over, by which time the
        // node it points at may have gone with a deleted ancestor.
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: false);
        _state.AddRootNode(node.RootNode);
        node.RootNode.Children.Add(node);

        Action? scrollToNode = null;
        _dataManagementService
            .ShowMessageToast(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Do<Action>(callback => scrollToNode = callback))
            .Returns(Task.CompletedTask);

        await _coordinator.TryConfirmEditAsync(node, pendingValues);
        Assert.NotNull(scrollToNode);
        node.RootNode.Children.Remove(node);

        // Act
        scrollToNode();

        // Assert
        _builder.Expansion.DidNotReceiveWithAnyArgs().ExpandToNode(default!);
        _builder.Scrolling.DidNotReceiveWithAnyArgs().RequestScrollToNode(default!);
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenTheToastIsClosed_ScrollsToTheNode()
    {
        // Arrange
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: false);
        _state.AddRootNode(node.RootNode);
        node.RootNode.Children.Add(node);

        Action? scrollToNode = null;
        _dataManagementService
            .ShowMessageToast(Arg.Any<LogLevel>(), Arg.Any<string>(), Arg.Do<Action>(callback => scrollToNode = callback))
            .Returns(Task.CompletedTask);

        await _coordinator.TryConfirmEditAsync(node, pendingValues);
        Assert.NotNull(scrollToNode);

        // Act
        scrollToNode();

        // Assert
        _builder.Expansion.Received(1).ExpandToNode(node);
        _builder.Scrolling.Received(1).RequestScrollToNode(node);
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenChangesValid_AssignsPendingValuesAndReturnsTrue()
    {
        // Arrange
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: true);

        // Act
        var result = await _coordinator.TryConfirmEditAsync(node, pendingValues);

        // Assert
        Assert.True(result);
        Assert.Equal(PendingName, node.Name);
        Assert.Equal(DataPortDirection.In, node.GetRequiredSystemProperty<DataPortDirection>().TypedValue);
    }

    [Fact]
    public async Task TryConfirmEditAsync_WhenChangesValid_DoesNotShowToast()
    {
        // Arrange
        var (node, pendingValues) = CreateDataPortNodeWithPendingDirectionChange(canSetDirection: true);

        // Act
        await _coordinator.TryConfirmEditAsync(node, pendingValues);

        // Assert
        await _dataManagementService.DidNotReceiveWithAnyArgs().ShowMessageToast(default, default!, default!);
    }
}
