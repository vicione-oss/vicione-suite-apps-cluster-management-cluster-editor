using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortEditingCoordinatorTests
{
    private readonly ITreeBuilder _builder;
    private readonly DataPortEditingCoordinator _coordinator;
    private readonly IClusterEditorManagementInternal _dataManagementService;
    private readonly DataPortTreeState _state;

    public DataPortEditingCoordinatorTests()
    {
        _dataManagementService = Substitute.For<IClusterEditorManagementInternal>();
        _builder = Substitute.For<ITreeBuilder>();
        _state = new DataPortTreeState { Builder = _builder };
        _coordinator = new DataPortEditingCoordinator(_dataManagementService, _state);
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
}
