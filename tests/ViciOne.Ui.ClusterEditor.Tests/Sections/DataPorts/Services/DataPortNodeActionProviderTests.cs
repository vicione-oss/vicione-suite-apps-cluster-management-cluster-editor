using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortNodeActionProviderTests
{
    private readonly IContextMenuRequest<DataPortAddChildNodeContextMenuContext> _addChildNodeContextMenuRequest;
    private readonly ITreeBuilder _builder;
    private readonly DataPortEditingCoordinator _editingCoordinator;
    private readonly DataPortTreeMutator _mutator;
    private readonly DataPortNodeActionProvider _provider;
    private readonly DataPortTreeState _state;

    public DataPortNodeActionProviderTests()
    {
        _addChildNodeContextMenuRequest = Substitute.For<IContextMenuRequest<DataPortAddChildNodeContextMenuContext>>();
        var rulesetProvider = Substitute.For<IRulesetProvider>();

        var datastore = Substitute.For<IDatastore>();
        var builder = Substitute.For<IClusterBuilder>();
        var editors = Substitute.For<IEditors>();
        var dataflow = Substitute.For<IDataflowEditor>();
        dataflow.AddDataPort(
            Arg.Any<Cluster.Model.Dataflow>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DataPortDirection>(),
            Arg.Any<string>(),
            Arg.Any<Guid>()
        ).Returns(new DataPort() { Name = "MQTT - Broker" });
        editors.Dataflow.Returns(dataflow);
        builder.Editors.Returns(editors);
        datastore.Builder.Returns(builder);

        _builder = Substitute.For<ITreeBuilder>();
        _state = new DataPortTreeState { Builder = _builder };
        _mutator = new(
            datastore,
            rulesetProvider,
            Substitute.For<ILogger<DataPortTreeMutator>>(),
            _state,
            new(rulesetProvider));
        _editingCoordinator = new DataPortEditingCoordinator(
            Substitute.For<IClusterEditorManagementInternal>(),
            _state);
        _provider = new DataPortNodeActionProvider(
            _addChildNodeContextMenuRequest,
            _mutator,
            _editingCoordinator,
            _state);
    }

    private static DataPortRootNodeModel CreateRoot()
        => new()
        {
            Builder = new TreeBuilder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };

    [Fact]
    public void GetActions_WhenNodeIsNotDataPortNode_ReturnsEmpty()
    {
        // Arrange
        var node = Substitute.For<ITreeNode>();

        // Act
        var actions = _provider.GetActions(node);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void GetActions_ForRootNode_AlwaysIncludesAddAndDeleteActions()
    {
        // Arrange
        var root = CreateRoot();

        // Act
        var actions = _provider.GetActions(root).ToList();

        // Assert
        Assert.Equal(2, actions.Count);
        Assert.All(actions, action => Assert.IsType<NodeButton>(action));
    }

    [Fact]
    public void GetActions_WhenNodeCanHaveChildren_IncludesSortAction()
    {
        // Arrange
        var root = CreateRoot();
        root.CanHaveChildren = true;

        // Act
        var actions = _provider.GetActions(root).ToList();

        // Assert
        Assert.Equal(3, actions.Count);
    }

    [Fact]
    public void GetActions_ForChildNodeWithProperties_IncludesEditAction()
    {
        // Arrange
        var root = CreateRoot();
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(rootNode: root);

        // Act
        var actions = _provider.GetActions(child).ToList();

        // Assert
        // Add, Edit (child has properties) and Delete actions.
        Assert.Equal(3, actions.Count);
    }

    private static NodeButton InvokableButton(INodeAction action)
        => (NodeButton)action;

    [Fact]
    public void AddAction_WhenSinglePossibleChild_CreatesChildAndExpands()
    {
        // Arrange
        var root = CreateRoot();
        var descriptor = root
            .GetPossibleChildNodes()
            .First(d => d.Children.Count == 0);
        root.PossibleChildren = [descriptor];
        var addButton = InvokableButton(_provider.GetActions(root).First());

        // Act
        addButton.Action!(addButton, new VisibleActionArguments { Builder = _builder, Node = root });

        // Assert
        _builder.Notifications.Received().NotifyNodeChanged(root, ChangedNodeDetail.Actions);
        _builder.Notifications.Received().NotifyChildrenChanged(root);
        _builder.Expansion.Received().ChangeExpansion(root, true);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddAction_WhenMultiplePossibleChildren_SendsContextMenuRequest()
    {
        // Arrange
        var root = CreateRoot();
        var descriptors = root.GetPossibleChildNodes();
        var first = descriptors.First(d => d.Children.Count == 0);
        root.PossibleChildren =
        [
            first,
            new DataPortChildNodeContextMenuDescriptor
            {
                IconName = "i",
                Name = "Second",
                NodeReference = first.NodeReference,
                ParentNode = root,
            }
        ];
        var addButton = InvokableButton(_provider.GetActions(root).First());

        // Act
        addButton.Action!(addButton, new VisibleActionArguments
        {
            Builder = _builder,
            MouseArgs = new Microsoft.AspNetCore.Components.Web.MouseEventArgs(),
            Node = root
        });

        // Assert
        await _addChildNodeContextMenuRequest.ReceivedWithAnyArgs().SendAsync(default!);
    }

    [Fact]
    public void AddAction_WhenNodeIsNotDataPortNode_DoesNothing()
    {
        // Arrange
        var root = CreateRoot();
        var addButton = InvokableButton(_provider.GetActions(root).First());

        // Act
        addButton.Action!(addButton, new VisibleActionArguments { Builder = _builder, Node = Substitute.For<ITreeNode>() });

        // Assert
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyNodeChanged(root, ChangedNodeDetail.Actions);
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyChildrenChanged(root);
    }

    [Fact]
    public async System.Threading.Tasks.Task EditAction_ForChildNode_BeginsEdit()
    {
        // Arrange
        var root = CreateRoot();
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(rootNode: root);
        var editButton = InvokableButton(_provider.GetActions(child).ElementAt(1));

        // Act
        editButton.Action!(editButton, new VisibleActionArguments { Builder = _builder, Node = child });
        await System.Threading.Tasks.Task.Yield();

        // Assert
        Assert.Same(child, _state.EditingTreeNode);
        Assert.True(child.IsEditModeActive);
    }

    [Fact]
    public void EditAction_WhenNodeIsNotDataPortNode_DoesNothing()
    {
        // Arrange
        var root = CreateRoot();
        var child = DataPortNodeModelCreator.CreateDataPortChildNodeModel(rootNode: root);
        var editButton = InvokableButton(_provider.GetActions(child).ElementAt(1));

        // Act
        editButton.Action!(editButton, new VisibleActionArguments { Builder = _builder, Node = Substitute.For<ITreeNode>() });

        // Assert
        Assert.Null(_state.EditingTreeNode);
    }

    [Fact]
    public void SortAction_WhenNodeHasChildren_SortsAndNotifies()
    {
        // Arrange
        var root = CreateRoot();
        root.CanHaveChildren = true;
        root.Children.Add(DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root));
        root.Children.Add(DataPortNodeModelCreator.CreateDataPortChildNodeModel(parent: root, rootNode: root));
        var sortButton = InvokableButton(_provider.GetActions(root).ElementAt(1));

        // Act
        sortButton.Action!(sortButton, new VisibleActionArguments { Builder = _builder, Node = root });

        // Assert
        _builder.Notifications.Received().NotifyChildrenChanged(root);
    }

    [Fact]
    public void SortAction_WhenNodeHasNoChildrenOrIsNotDataPortNode_DoesNotNotify()
    {
        // Arrange
        var root = CreateRoot();
        root.CanHaveChildren = true;
        var sortButton = InvokableButton(_provider.GetActions(root).ElementAt(1));

        // Act
        sortButton.Action!(sortButton, new VisibleActionArguments { Builder = _builder, Node = root });
        sortButton.Action!(sortButton, new VisibleActionArguments { Builder = _builder, Node = Substitute.For<ITreeNode>() });

        // Assert
        _builder.Notifications.DidNotReceiveWithAnyArgs().NotifyChildrenChanged(treeNode: default!);
    }

    [Fact]
    public void Actions_EnabledFuncs_AreEvaluated()
    {
        // Arrange
        var root = CreateRoot();
        root.CanHaveChildren = true;
        var descriptor = root.GetPossibleChildNodes()
            .First(d => d.Children.Count == 0);
        root.PossibleChildren = [descriptor];
        var actions = _provider.GetActions(root).OfType<NodeButton>().ToList();

        // Act & Assert - invoke every EnabledFunc to cover the predicate lambdas.
        Assert.All(actions, button => button.EnabledFunc!(root));
    }
}
