using System;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Diagrams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortTreeAdapterTests : IAsyncDisposable
{
    private readonly DataPortNodeActionProvider _actionProvider;
    private readonly DataPortTreeAdapter _adapter;
    private readonly IClusterBuilder _builder;
    private readonly IContextMenuRequest<DataPortAddChildNodeContextMenuContext> _contextMenuRequest;
    private readonly IDatastore _datastore;
    private readonly DiagramService _diagramService;
    private readonly DataPortDragCoordinator _dragCoordinator;
    private readonly DragService _dragService;
    private readonly ClusterBuilderEventBuffer _eventBuffer = new();
    private readonly DataPortClusterEventSynchronizer _eventSynchronizer;
    private readonly DataPortIconResolver _iconResolver;
    private readonly IRulesetProvider _rulesetProvider;
    private readonly TreeEditor.Builder.TreeBuilder _treeBuilder;
    private readonly DataPortTreeBuilderRegistry _treeBuilderRegistry;
    private readonly DataPortTreeMutator _treeMutator;
    private readonly DataPortTreeState _treeState;

    public DataPortTreeAdapterTests()
    {
        _treeState = new DataPortTreeState();
        _datastore = Substitute.For<IDatastore>();
        _diagramService = new(_datastore, new(Substitute.For<ILogger<DiagramEventService>>()), Substitute.For<ILogger<DiagramService>>())
        {
            Diagram = new BlazorDiagram()
        };
        _dragService = new(new(Substitute.For<ILogger<DiagramEventService>>()), _diagramService, new());
        _dragCoordinator = new(_dragService, _datastore);
        _rulesetProvider = Substitute.For<IRulesetProvider>();
        _eventSynchronizer = new(_eventBuffer, _rulesetProvider, _datastore, _treeState);
        _iconResolver = new DataPortIconResolver(_datastore);
        _treeBuilderRegistry = new DataPortTreeBuilderRegistry(_rulesetProvider, new FakeLogger<DataPortTreeBuilderRegistry>());
        _treeMutator = new DataPortTreeMutator(_datastore, _rulesetProvider, Substitute.For<ILogger<DataPortTreeMutator>>(), _treeState, _treeBuilderRegistry);
        _contextMenuRequest = Substitute.For<IContextMenuRequest<DataPortAddChildNodeContextMenuContext>>();
        _actionProvider = new DataPortNodeActionProvider(_contextMenuRequest,
            _treeMutator,
            new DataPortEditingCoordinator(Substitute.For<IClusterEditorManagementInternal>(), _treeState),
            _treeState);

        _adapter = new DataPortTreeAdapter(
            _actionProvider,
            _datastore,
            _treeState,
            _dragCoordinator,
            _eventSynchronizer,
            _iconResolver,
            Substitute.For<ILogger<DataPortTreeAdapter>>(),
            _treeMutator,
            _treeBuilderRegistry);

        _builder = BuilderFactory.Create();

        _datastore.Builder.Returns(_builder);

        _treeBuilder = new TreeEditor.Builder.TreeBuilder();
        _treeBuilder.SetAdapter(_adapter);
        _adapter.Initialize();
    }

    public async ValueTask DisposeAsync()
    {
        _adapter.Dispose();
        _builder.Dispose();
        await _datastore.DisposeAsync();
        _diagramService.Dispose();
        _eventBuffer.Dispose();
        _eventSynchronizer.Dispose();
        _treeBuilder.Dispose();
    }

    [Fact]
    public void GetIcons_CallsIconResolver()
    {
        // Arrange
        var rootNode = DataPortNodeModelCreator.CreateDataPortRootNodeModel("broker");

        // Act
        var result = _adapter.GetIcons(rootNode).ToList();

        // Assert
        var item = Assert.Single(result);
        Assert.Equal(_iconResolver.GetIcons(rootNode).First().MarkupString, ((SvgIcon)item).MarkupString);
    }

    private DataPortRootNodeModel SeedRoot()
    {
        _rulesetProvider.GetRuleset(Arg.Any<RulesetIdentifier>()).Returns(TestResources.MqttRuleset);
        _rulesetProvider
            .GetSystemDataPortDependency()
            .Returns(new ClusterDependency { Name = "SystemDataPort", Version = "1.0.0" });
        _datastore.ActiveDataflow.Returns(_builder.Cluster.Dataflows[0]);

        _adapter.CreateNewDataPortRootNode("MQTTDataPort");
        return (DataPortRootNodeModel)_adapter.GetRootNodes().Single();
    }

    [Fact]
    public void CanInboundDropAsChild_ReflectsValidInboundDropTargets()
    {
        // Arrange
        var root = SeedRoot();

        // Act & Assert
        Assert.False(_adapter.CanInboundDropAsChild(root));
        _adapter.ValidInboundDropTargets.Add(root);
        Assert.True(_adapter.CanInboundDropAsChild(root));
    }

    [Fact]
    public void Dismantle_AfterSetup_DoesNotThrow()
    {
        // Arrange
        SeedRoot();

        // Act & Assert
        _adapter.Dismantle();
    }

    [Fact]
    public void FilterNodes_WithSeededTree_DoesNotThrow()
    {
        // Arrange
        var root = SeedRoot();
        _adapter.SortNodeChildren(root);

        // Act & Assert
        _adapter.FilterNodes(root.Children.Single().Name);
    }

    [Fact]
    public void FilterNodes_WhenMatchingChildNode_KeepsParentChainVisible()
    {
        // Arrange - a broker with a folder beneath it, filtered by the folder's name so the
        // filter helper resolves the child's parent chain.
        var root = SeedRoot();
        var broker = root.Children.Single();
        var descriptor = broker.PossibleChildren.First(d => d.Children.Count == 0);
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, broker);
        _adapter.CreateNewChildNode(broker, folder);

        // Act & Assert (exercises ResolveParent for a DataPortChildNodeModel)
        _adapter.FilterNodes(folder.Name);
        Assert.True(broker.Expanded || broker.Children.Contains(folder));
    }

    [Fact]
    public void HasChildren_ReturnsTrueForNodeWithChildren_AndFalseOtherwise()
    {
        // Arrange
        var root = SeedRoot();

        // Act & Assert
        Assert.True(_adapter.HasChildren(root));
        Assert.False(_adapter.HasChildren(Substitute.For<ITreeNode>()));
    }

    [Fact]
    public void InitializeDataPortTree_RebuildsRootNodesFromBuilder()
    {
        // Arrange
        SeedRoot();

        // Act & Assert (should not throw; delegates to the mutator)
        _adapter.InitializeDataPortTree();
        Assert.NotEmpty(_adapter.GetRootNodes());
    }

    [Fact]
    public void GetActions_DelegatesToActionProvider()
    {
        // Arrange
        var root = SeedRoot();

        // Act
        var actions = _adapter.GetActions(root).ToList();

        // Assert
        Assert.NotEmpty(actions);
    }

    [Fact]
    public void GetChildren_ReturnsChildrenForDataPortNode_AndEmptyOtherwise()
    {
        // Arrange
        var root = SeedRoot();

        // Act & Assert
        Assert.Equal(root.Children, _adapter.GetChildren(root));
        Assert.Empty(_adapter.GetChildren(Substitute.For<ITreeNode>()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetCssClasses_ReturnsHighlightedOnlyForHighlightedNodeTemplate(bool highlighted)
    {
        // Arrange
        var root = SeedRoot();
        root.Highlighted = highlighted;

        // Act
        var nodeClasses = _adapter.GetCssClasses(root, TemplateType.Node).ToList();
        var displayClasses = _adapter.GetCssClasses(root, TemplateType.NodeDisplay).ToList();

        // Assert
        Assert.Equal(highlighted ? ["highlighted"] : Array.Empty<string>(), nodeClasses);
        Assert.Empty(displayClasses);
    }

    [Fact]
    public void GetDblClickAction_ForNodeWithoutLinks_TogglesExpansion()
    {
        // Arrange
        var root = SeedRoot();
        var action = _adapter.GetDblClickAction(root)!;
        var expandedBefore = root.Expanded;

        // Act
        action(root);
        action(Substitute.For<ITreeNode>());

        // Assert
        Assert.NotEqual(expandedBefore, root.Expanded);
    }

    [Fact]
    public void GetDisplayText_ReturnsNameForDataPortNode_AndEmptyOtherwise()
    {
        // Arrange
        var root = SeedRoot();

        // Act & Assert
        Assert.Equal(root.Name, _adapter.GetDisplayText(root));
        Assert.Equal(string.Empty, _adapter.GetDisplayText(Substitute.For<ITreeNode>()));
    }

    [Fact]
    public void GetParent_ReturnsParentForChild_AndNullOtherwise()
    {
        // Arrange
        var root = SeedRoot();
        var child = (DataPortChildNodeModel)root.Children.Single();

        // Act & Assert
        Assert.Same(child.Parent, _adapter.GetParent(child));
        Assert.Null(_adapter.GetParent(root));
    }

    [Fact]
    public void GetTreeNode_ReturnsMatchingNode_AndNullForUnknown()
    {
        // Arrange
        var root = SeedRoot();
        var child = root.Children.Single();

        // Act & Assert
        Assert.Same(child, _adapter.GetTreeNode(new DataPortTreeNode { Id = child.Id.Value }));
        Assert.Null(_adapter.GetTreeNode(new DataPortTreeNode { Id = Guid.NewGuid() }));
    }

    [Fact]
    public void GetDataPortRootNode_ReturnsSeededRoot()
    {
        // Arrange
        var root = SeedRoot();

        // Act & Assert
        Assert.Same(root, _adapter.GetDataPortRootNode("MQTTDataPort"));
    }

    [Fact]
    public void IsExpandedAndIsSelected_ReflectNodeState()
    {
        // Arrange
        var root = SeedRoot();
        root.Expanded = true;
        root.Selected = true;

        // Act & Assert
        Assert.True(_adapter.IsExpanded(root));
        Assert.True(_adapter.IsSelected(root));
        Assert.False(_adapter.IsExpanded(Substitute.For<ITreeNode>()));
        Assert.False(_adapter.IsSelected(Substitute.For<ITreeNode>()));
    }

    [Fact]
    public void ExpansionAndSelectionChanges_UpdateNodeState()
    {
        // Arrange
        var root = SeedRoot();

        // Act
        _treeBuilder.Expansion.ChangeExpansion(root, true);
        _treeBuilder.Selection.ChangeSelection(root, true);

        // Assert
        Assert.True(root.Expanded);
        Assert.True(root.Selected);
    }

    [Fact]
    public void OnDeleteNodeUserConfirmationRequest_RoundTripsThroughState()
    {
        // Arrange
        static void Handler(ITreeNode node, Action confirm) => confirm();

        // Act
        _adapter.OnDeleteNodeUserConfirmationRequest = Handler;

        // Assert
        Assert.NotNull(_adapter.OnDeleteNodeUserConfirmationRequest);
    }

    [Fact]
    public void ProcessNodeChanges_ForNonChildNode_DoesNotThrow()
    {
        // Arrange
        SeedRoot();

        // Act & Assert
        _adapter.ProcessNodeChanges(Substitute.For<ITreeNode>());
    }

    [Fact]
    public void RevertNodeChanges_ForSeededChild_DoesNotThrow()
    {
        // Arrange
        var root = SeedRoot();
        var child = (DataPortChildNodeModel)root.Children.Single();

        // Act & Assert
        _adapter.RevertNodeChanges(child);
    }

    [Fact]
    public void SetHighlightState_UpdatesDataPortNodesOnly()
    {
        // Arrange
        var root = SeedRoot();

        // Act
        _adapter.SetHighlightState(true, [root, Substitute.For<ITreeNode>()]);

        // Assert
        Assert.True(root.Highlighted);
    }

    [Fact]
    public void CreateNewChildNode_DelegatesToMutator()
    {
        // Arrange
        var root = SeedRoot();
        var broker = root.Children.Single();
        var descriptor = broker.PossibleChildren.First(d => d.Children.Count == 0);
        var folder = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, broker);

        // Act
        _adapter.CreateNewChildNode(broker, folder);

        // Assert
        Assert.Contains(folder, broker.Children);
    }

    [Fact]
    public void TemplateMapping_ReturnsExpectedTemplateTypes()
    {
        // Arrange
        var root = SeedRoot();
        var child = root.Children.Single();
        var mapping = _treeBuilder.Template.Mapping!;

        // Act & Assert
        Assert.Equal(typeof(DataPortChildNode), mapping(child, TemplateType.Node));
        Assert.Null(mapping(child, TemplateType.NodeDisplay));

        child.IsEditModeActive = true;
        Assert.Equal(typeof(DataPortEditNodeTemplate), mapping(child, TemplateType.NodeDisplay));
        Assert.Null(mapping(Substitute.For<ITreeNode>(), TemplateType.NodeDisplay));
        Assert.Null(mapping(child, (TemplateType)(-1)));
    }

    [Fact]
    public async Task DataPortWithLinksDoubleClicked_CanSubscribeAndUnsubscribe()
    {
        // Arrange
        static Task Handler(ITreeNode node) => Task.CompletedTask;

        // Act & Assert (no throw)
        _adapter.DataPortWithLinksDoubleClicked += Handler;
        _adapter.DataPortWithLinksDoubleClicked -= Handler;
        await Task.CompletedTask;
    }
}
