using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.Extensions;
using ViciOne.Ui.Blazor.Components.ContextMenu.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components.Localization;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.ContextMenu;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "#1601")]
internal sealed partial class DataPortTreeAdapter : TreeAdapter, IDisposable
{
    private const string DirectionPropertyName = "Direction";

    private static readonly CompositeFormat s_compositeNodeActionAdd = CompositeFormat.Parse(DataPortSection.NodeActionAdd);

    private readonly IContextMenuRequest<DataPortAddChildNodeContextMenuContext> _addChildNodeContextMenuRequest;
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly IClusterEditorManagementInternal _dataManagementService;
    private readonly IDataPortTreeIconProvider _dataPortTreeIconProvider;
    private readonly IDatastore _datastore;
    private readonly DragService _dragService;
    private DataPortNodeModel? _editingTreeNode;
    private bool _isDeletionInProgress;
    private readonly ILogger<DataPortTreeAdapter> _logger;
    private readonly IRulesetProvider _rulesetProvider;
    private readonly Dictionary<string, TreeBuilder.TreeBuilder> _treeBuilders = [];

    public List<ITreeNode> ValidInboundDropTargets { get; } = [];

    public event Action<ITreeNode>? DataPortWithLinksDoubleClicked;

    public DataPortTreeAdapter(
        IContextMenuRequest<DataPortAddChildNodeContextMenuContext> addChildNodeContextMenuRequest,
        ClusterBuilderEventBuffer clusterBuilderEventBuffer,
        IDatastore datastore,
        DragService dragService,
        IRulesetProvider rulesetProvider,
        ILogger<DataPortTreeAdapter> logger,
        IClusterEditorManagementInternal dataManagementService,
        IDataPortTreeIconProvider dataPortTreeIconProvider)
    {
        _addChildNodeContextMenuRequest = addChildNodeContextMenuRequest;
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _datastore = datastore;
        _dragService = dragService;
        _rulesetProvider = rulesetProvider;
        _logger = logger;
        _dataManagementService = dataManagementService;

        _clusterBuilderEventBuffer.DataPortsRemoved += OnDataPortsRemoved;
        _clusterBuilderEventBuffer.DataPortPropertiesChanged += OnDataPortPropertiesChanged;
        _clusterBuilderEventBuffer.TreeNodesRemoved += OnTreeNodesRemoved;
        _clusterBuilderEventBuffer.DataPortTreeNodeLinksAdded += OnDataPortTreeNodeLinksChanged;
        _clusterBuilderEventBuffer.DataPortTreeNodeLinksRemoved += OnDataPortTreeNodeLinksChanged;
        _dataPortTreeIconProvider = dataPortTreeIconProvider;
    }

    private async void AddNewNodeAsync(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        if (dpNode.PossibleChildren.Count == 1)
        {
            CreateNewChildNode(dpNode, dpNode.PossibleChildren[0]);
            Builder.Expansion.ChangeExpansion(dpNode, true);
            return;
        }

        await _addChildNodeContextMenuRequest.SendAsync(new DataPortAddChildNodeContextMenuContext
        {
            MouseEventArgs = e.MouseArgs!,
            ParentNode = dpNode,
            PossibleChildren = SortNodes([.. dpNode.PossibleChildren.Cast<DataPortNodeModel>()], true).Cast<DataPortChildNodeModel>(),
        });
    }

    public override bool CanInboundDropAsChild(ITreeNode target)
        => ValidInboundDropTargets.Contains(target);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to build tree for DataPort {DataPortId}.")]
    public static partial void CreateDataPortTreeFailed(ILogger logger, Exception ex, Guid dataPortId);

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;

        Builder.DragAndDrop.DragEnded -= OnDragEnded;
        Builder.DragAndDrop.Dragged -= OnDragged;
        Builder.DragAndDrop.DragStarted -= OnDragStarted;
    }

    public void Dispose()
    {
        _clusterBuilderEventBuffer.DataPortsRemoved -= OnDataPortsRemoved;
        _clusterBuilderEventBuffer.DataPortPropertiesChanged -= OnDataPortPropertiesChanged;
        _clusterBuilderEventBuffer.TreeNodesRemoved -= OnTreeNodesRemoved;
        _clusterBuilderEventBuffer.DataPortTreeNodeLinksAdded -= OnDataPortTreeNodeLinksChanged;
        _clusterBuilderEventBuffer.DataPortTreeNodeLinksRemoved -= OnDataPortTreeNodeLinksChanged;
    }

    private async void EditNode(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        if (_editingTreeNode is not null && _editingTreeNode.IsEditModeActive)
        {
            if (_editingTreeNode.HasChangedProperties)
            {
                await _dataManagementService.ShowMessageToast(LogLevel.Warning, SharedSectionText.UnsavedNodeChanges, () => _editingTreeNode.ScrollToNode(Builder));
                return;
            }

            _editingTreeNode.IsEditModeActive = false;
            Builder.Notifications.NotifyNodeChanged(_editingTreeNode, ChangedNodeDetail.None);
        }

        if (dpNode.Id != _editingTreeNode?.Id || !_editingTreeNode.IsEditModeActive)
        {
            dpNode.IsEditModeActive = true;
            dpNode.HasChangedProperties = false;
        }

        _editingTreeNode = dpNode;
        Builder.Notifications.NotifyNodeChanged(dpNode, ChangedNodeDetail.None);
    }

    private static DataPortNodeModel? FindTreeNode(DataPortNodeModel parentNode, GuidNodeIdentifier id)
    {
        if (parentNode.Id == id)
            return parentNode;

        foreach (var node in parentNode.Children)
        {
            var treeNode = FindTreeNode(node, id);
            if (treeNode is not null)
                return treeNode;
        }

        return null;
    }

    public override IEnumerable<INodeAction> GetActions(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            return [];

        var result = new List<INodeAction>();

        var canHaveAdditionalChildren = dataPortNode.PossibleChildren.Any();

        result.Add(new NodeButton()
        {
            Action = AddNewNodeAsync,
            Description = GetNodeActionAddDescription(dataPortNode),
            EnabledFunc = (_) => canHaveAdditionalChildren,
            Icon = new TreeEditorMonochromeIcon(MonochromeIconName.PlusSlim, MonochromeIconSize.Small),
        });

        if (dataPortNode is DataPortChildNodeModel childNode && childNode.Properties.Count > 0)
        {
            result.Add(new NodeButton()
            {
                Action = EditNode,
                Description = CompositeFormats.EditSomething(TechnicalTerms.Node),
                EnabledFunc = (_) => !childNode.IsEditModeActive,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Edit, MonochromeIconSize.Small),
            });
        }

        if (dataPortNode.CanHaveChildren)
        {
            result.Add(new NodeButton()
            {
                Action = SortChildNodes,
                Description = DataPortSection.NodeActionSortChildren,
                EnabledFunc = (node) => dataPortNode.Children.Count > 1,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.SortChildren, MonochromeIconSize.Small),
            });
        }

        result.Add(new NodeButton()
        {
            Action = DeleteDataPortTreeNode,
            Description = CompositeFormats.DeleteSomething(TechnicalTerms.Node),
            EnabledFunc = (_) => true,
            Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Delete, MonochromeIconSize.Small),
        });

        return result;
    }

    public override IEnumerable<ITreeNode> GetChildren(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            return Enumerable.Empty<DataPortNodeModel>();

        return dataPortNode.Children;
    }

    public override IEnumerable<string> GetCssClasses(ITreeNode node, TemplateType templateType)
    {
        if (templateType != TemplateType.Node)
            return [];

        if (node is DataPortNodeModel dataPortNode && dataPortNode.Highlighted)
            return ["highlighted"];

        return [];
    }

    public DataPortRootNodeModel? GetDataPortRootNode(string rulesetRootId)
        => _dataPorts.FirstOrDefault(k => k.Builder.Ruleset.Root?.Id.Equals(rulesetRootId, StringComparison.OrdinalIgnoreCase) ?? false);

    public override Action<ITreeNode>? GetDblClickAction(ITreeNode node)
        => n =>
        {
            if (n is not DataPortNodeModel dpNode)
                return;

            if (_datastore.Builder.Cache.DataPortTreeNodeIds.ContainsKey(dpNode.Id.Value) && _datastore.Builder.Cache.DataPortTreeNodeIds[dpNode.Id.Value].Links.Any())
                DataPortWithLinksDoubleClicked?.Invoke(node);
            else
                Builder.Expansion.ChangeExpansion(node, !((DataPortNodeModel)node).Expanded);
        };

    public override string GetDisplayText(ITreeNode node)
    {
        if (node is DataPortNodeModel dpNode)
            return dpNode.DisplayText;

        return string.Empty;
    }

    public override IEnumerable<IIcon> GetIcons(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataPortNode)
            yield break;

        if (dataPortNode is DataPortRootNodeModel rootNodeModel)
        {
            var iconName = string.IsNullOrEmpty(dataPortNode.Icon)
                ? (dataPortNode.AvailableIcons.Count > 0 ? dataPortNode.AvailableIcons[0] : null)
                : dataPortNode.Icon;
            if (!string.IsNullOrEmpty(iconName))
            {
                var icon = _dataPortTreeIconProvider.GetSvgIcon(rootNodeModel.Builder, iconName) ?? _dataPortTreeIconProvider.GetSvgIcon(rootNodeModel.Builder, "server");
                if (!string.IsNullOrEmpty(icon))
                    yield return new SvgIcon(icon);
            }

            yield break;
        }

        if (dataPortNode is DataPortChildNodeModel { NodeReference: not null } childNode)
        {
            switch (childNode.Icon)
            {
                case null:
                    {
                        var nodeType = childNode.GetRootNode().Builder.NodeTypes[childNode.NodeReference!.Id];
                        var iconName = nodeType.Icons.FirstOrDefault();
                        if (iconName is not null)
                        {
                            var icon = _dataPortTreeIconProvider.GetSvgIcon(childNode.RootNode.Builder, iconName);
                            if (!string.IsNullOrEmpty(icon))
                                yield return new SvgIcon(icon);
                        }
                        break;
                    }

                case not null when childNode.Icon.Equals("datapoint", StringComparison.OrdinalIgnoreCase):
                    {
                        var icon = _dataPortTreeIconProvider.GetDataPointIcon(childNode, 24, _datastore.Builder.Cache);
                        if (icon is not null)
                            yield return icon;
                        break;
                    }

                default:
                    {
                        var icon = _dataPortTreeIconProvider.GetSvgIcon(childNode.RootNode.Builder, childNode.Icon);
                        if (!string.IsNullOrEmpty(icon))
                            yield return new SvgIcon(icon);
                        break;
                    }
            }
        }
    }

    private static string GetNodeActionAddDescription(DataPortNodeModel dataPortNode)
        => dataPortNode.PossibleChildren.Count == 1
            ? string.Format(CultureInfo.InvariantCulture, s_compositeNodeActionAdd, dataPortNode.PossibleChildren[0].DisplayText)
            : string.Format(CultureInfo.InvariantCulture, s_compositeNodeActionAdd, DataPortSection.NewChild);

    private TreeBuilder.TreeBuilder GetOrCreateTreeBuilder(RulesetIdentifier rulesetId)
    {
        if (!_treeBuilders.TryGetValue(rulesetId.Key, out var treeBuilder))
        {
            treeBuilder = new TreeBuilder.TreeBuilder(_rulesetProvider.GetRuleset(rulesetId));
            _treeBuilders.Add(rulesetId.Key, treeBuilder);
        }

        return treeBuilder;
    }

    private TreeBuilder.TreeBuilder GetOrCreateTreeBuilder(string dataPortCategory, string rulesetIdentifier)
        => GetOrCreateTreeBuilder(new RulesetIdentifier(dataPortCategory, rulesetIdentifier));

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is not DataPortChildNodeModel dataPortChildNode)
            return null;

        return dataPortChildNode.Parent;
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => _dataPorts;

    public IEnumerable<ITreeNode> GetSelectedNodes()
    {
        var result = new List<ITreeNode>();
        result.AddRange(_dataPorts.Where(dprn => dprn.Selected));

        foreach (var dataPortNode in _dataPorts.SelectMany(dprn => dprn.Children).ToArray())
            result.AddRange(GetSelectedNodesRecursive(dataPortNode));

        return result;

        static IEnumerable<DataPortNodeModel> GetSelectedNodesRecursive(DataPortNodeModel node)
        {
            var result = new List<DataPortNodeModel>();
            if (node.Selected)
                result.Add(node);

            foreach (var child in node.Children)
                result.AddRange(GetSelectedNodesRecursive(child));

            return result;
        }
    }

    private static Type? GetTemplateMapping(ITreeNode node, TemplateType templateType)
        => templateType switch
        {
            TemplateType.NodeDisplay => node switch
            {
                DataPortNodeModel dataPortNode => dataPortNode.IsEditModeActive ? typeof(DataPortEditNodeTemplate) : null,
                _ => null
            },
            TemplateType.Node => typeof(DataPortChildNode),
            _ => null
        };

    public ITreeNode? GetTreeNode(DataPortTreeNode dataPortTreeNode)
    {
        DataPortNodeModel? treeNode = null;
        foreach (var rootNode in _dataPorts)
        {
            treeNode = FindTreeNode(rootNode, new GuidNodeIdentifier(dataPortTreeNode.Id));
            if (treeNode is not null)
                break;
        }

        return treeNode;
    }

    public override bool HasChildren(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataportNode)
            return false;

        return dataportNode.Children.Count != 0;
    }

    public void Initialize()
    {
        _treeBuilders.Clear();

        // Create a treebuilder for each ruleset and create nodes for all existing DataPorts
        foreach (var rulesetId in _rulesetProvider.GetRulesetIdentifiers(DataPortCategory).ToArray())
            GetOrCreateTreeBuilder(rulesetId);
    }

    public void InitializeDataPortTree()
    {
        _dataPorts.Clear();

        // Create visual tree using DataPort with matching treebuilder
        foreach (var dataPort in _datastore.Builder.Cache.DataPorts)
            TryCreateDataPortTree(dataPort);

        Builder.Reset();
        Builder.Helper.Preload();
    }

    public override bool IsExpanded(ITreeNode treeNode)
    {
        if (treeNode is DataPortNodeModel dpNode)
            return dpNode.Expanded;

        return false;
    }

    public override bool IsSelected(ITreeNode treeNode)
    {
        if (treeNode is DataPortNodeModel dpNode)
            return dpNode.Selected;

        return false;
    }

    private void OnDataPortPropertiesChanged(IEnumerable<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> changedDataPortProperties)
    {
        if (_isDeletionInProgress)
            return;

        foreach (var (sender, e) in changedDataPortProperties)
        {
            if (e.PropertyName != DirectionPropertyName)
                continue;

            if (sender is not DataPort dataPort)
                continue;

            var dataPortNode = _dataPorts.FindNode(dataPort.Id);

            if (dataPortNode is null)
                continue;

            // At this point the icons of the children must change. This should be done by a call to
            // Events.InvokeChildrenChanged(dataPortNode), but currently this method doesn't
            // update the icons of the children. Therefore we do this recursively here until the bug is fixed.
            UpdateChildrenIconsRecursively(dataPortNode);
        }
    }

    private void OnDataPortsRemoved(IEnumerable<(Cluster.Model.Dataflow Parent, DataPort DataPort)> dataPorts)
    {
        foreach (var dataPort in dataPorts)
        {
            var dataPortNode = _dataPorts.FindNode(dataPort.DataPort.Id);
            if (dataPortNode is null)
                continue;

            if (_editingTreeNode is not null && _editingTreeNode.Id == dataPortNode.Id)
                _editingTreeNode = null;

            dataPortNode.RootNode.Children.Remove(dataPortNode);

            if (dataPortNode.RootNode.Children.Count != 0)
            {
                dataPortNode.RootNode.PossibleChildren = [.. dataPortNode.RootNode.GetPossibleChildNodes()];
                Builder.Notifications.NotifyNodeChanged(dataPortNode.RootNode, ChangedNodeDetail.Actions | ChangedNodeDetail.Icons);
                Builder.Notifications.NotifyChildrenChanged(dataPortNode.RootNode);
            }
            else
            {
                _dataPorts.Remove(dataPortNode.RootNode);
                _datastore.Builder?.RemoveUnusedSystemDataPortDependency(_rulesetProvider);
                Builder.Notifications.NotifyRootNodesChanged();
            }
        }
    }

    private void OnDataPortTreeNodeLinksChanged(IEnumerable<Link> links)
    {
        if (_isDeletionInProgress)
            return;

        foreach (var link in links)
        {
            DataPortChildNodeModel? childNode = null;

            if (link.SourceDataPortTreeNode is not null)
                childNode = _dataPorts.FindNode(link.SourceDataPortTreeNode.Id);

            if (link.DestinationDataPortTreeNode is not null)
                childNode = _dataPorts.FindNode(link.DestinationDataPortTreeNode.Id);

            if (childNode is null)
                continue;

            Builder.Notifications.NotifyNodeChanged(childNode, ChangedNodeDetail.Icons);
        }
    }

    private void OnDragEnded()
        => _dragService.EndDragging();

    private void OnDragged(DragEventArgs e)
        => _dragService.OnPointerMove(e);

    private void OnDragStarted(IEnumerable<ITreeNode> treeNodes)
    {
        List<BlockNodeConnector> possibleTargetConnectors = [];

        foreach (var treeNode in treeNodes)
        {
            if (treeNode is not DataPortChildNodeModel dataPortChildNode)
                continue;

            var dataPortTreeNode = _datastore.Builder.Cache.DataPortTreeNodeIds.GetValueOrDefault(dataPortChildNode.Id.Value);
            if (dataPortTreeNode is null)
                continue;

            possibleTargetConnectors.AddRange(dataPortTreeNode.GetValidTargetConnectors(_datastore, dataPortChildNode));
        }

        _dragService.StartDragging(treeNodes.OfType<IDragable>(), possibleTargetConnectors, false);
    }

    private void OnExpansionChanged(ITreeNode node, bool expanded)
    {
        if (node is DataPortNodeModel dpNode)
            dpNode.Expanded = expanded;
    }

    private void OnSelectionChanged(ITreeNode node, bool selected)
    {
        if (node is DataPortNodeModel dpNode)
            dpNode.Selected = selected;
    }

    private void OnTreeNodesRemoved(IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)> treeNodes)
    {
        foreach (var treeNode in treeNodes)
        {
            var childNode = _dataPorts.FindNode(treeNode.DataPortTreeNode.Id);

            if (childNode is null)
                continue;

            if (childNode.Parent is not DataPortChildNodeModel parent)
                continue;

            if (_editingTreeNode is not null && _editingTreeNode.Id == childNode.Id)
                _editingTreeNode = null;

            parent.Children.Remove(childNode);
            parent.PossibleChildren = [.. parent.GetPossibleChildNodes()];
            Builder.Notifications.NotifyNodeChanged(parent, ChangedNodeDetail.Actions);
            Builder.Notifications.NotifyChildrenChanged(parent);
        }
    }

    public void ProcessNodeChanges(ITreeNode node)
    {
        if (_datastore.Builder is null)
            return;

        if (node is not DataPortChildNodeModel childNode)
            return;

        // Parent is root node so child is DataPort
        if (childNode.Parent is DataPortRootNodeModel)
        {
            var dataPort = _datastore.Builder.GetDataPort(childNode);

            // TODO - make it possible to change the engine in the node edit section
            _datastore.Builder.Editors.DataPort.SetName(dataPort, childNode.DisplayText);
            if (dataPort.Name != childNode.DisplayText)
                childNode.DisplayText = dataPort.Name;
            _datastore.Builder.Editors.DataPort.SetIcon(dataPort, childNode.Icon);
            _datastore.Builder.SetCustomDataPortProperties(dataPort, childNode);
            _datastore.Builder.SetSystemDataPortProperties(dataPort, childNode, Builder);
            return;
        }

        // Normal child eg. for MQTT Folder, Value
        var childNodeId = childNode.Id.Value;
        var clusterNode = _datastore.Builder.Cache.DataPortTreeNodes.FirstOrDefault(k => k.Id == childNodeId);
        if (clusterNode is null)
            return; // TODO: Error?!

        _datastore.Builder.Editors.DataPortTreeNode.SetName(clusterNode, childNode.DisplayText);
        if (clusterNode.Name != childNode.DisplayText)
            childNode.DisplayText = clusterNode.Name;
        _datastore.Builder.Editors.DataPortTreeNode.SetIcon(clusterNode, childNode.Icon);
        _datastore.Builder.SetSystemDataPortTreeNodeProperties(clusterNode, childNode);
        _datastore.Builder.SetCustomDataPortTreeNodeProperties(clusterNode, childNode);
    }

    public void RevertNodeChanges(DataPortChildNodeModel childNode)
    {
        if (_datastore.Builder is null)
            return;

        var root = childNode.GetRootNode();
        if (childNode.Parent.Id == root.Id)
        {
            var dataPort = _datastore.Builder.GetDataPort(childNode);
            childNode.AssignValuesAndProperties(dataPort);
        }
        else
        {
            var treeNode = _datastore.Builder.Cache.DataPortTreeNodeIds[childNode.Id.Value];
            childNode.AssignValuesAndProperties(treeNode, root.Builder);
        }
    }

    public void SetHighlightState(bool highlighted, IEnumerable<ITreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is DataPortNodeModel dataPortNode)
            {
                dataPortNode.Highlighted = highlighted;
                Builder.Notifications.NotifyNodeChanged(dataPortNode, ChangedNodeDetail.None);
            }
        }
    }

    public override void Setup()
    {
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
        Builder.Selection.SelectionChanged += OnSelectionChanged;

        Builder.DragAndDrop.EnableInbound = true;
        Builder.DragAndDrop.EnableOutbound = true;
        Builder.DragAndDrop.DisplayElementShadow = false;

        Builder.DragAndDrop.DragEnded += OnDragEnded;
        Builder.DragAndDrop.Dragged += OnDragged;
        Builder.DragAndDrop.DragStarted += OnDragStarted;

        Builder.Guidelines.Show = true;
        Builder.Template.Mapping = GetTemplateMapping;
    }

    private void SortChildNodes(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not DataPortNodeModel dpNode)
            return;

        if (dpNode.Children.Count == 0)
            return;

        SortChildren(dpNode, true);
        Builder.Notifications.NotifyChildrenChanged(dpNode);
    }

    internal void SortNodeChildren(DataPortNodeModel node)
    {
        if (node.PossibleChildren.Any())
        {
            if (node.GetRootNode().TryGetPathToNode(node, out var path) && path.Count > 1)
            {
                var parent = path.ElementAt(1);

                if (parent is DataPortNodeModel parentTreeNode)
                {
                    SortChildren(parentTreeNode, false);
                    Builder.Notifications.NotifyChildrenChanged(parentTreeNode);
                }
            }
        }
    }

    private void TryCreateDataPortTree(DataPort dataPort)
    {
        try
        {
            var treeBuilder = _treeBuilders.Values.FirstOrDefault(k => dataPort.RulesetId.Equals(k.Ruleset.Root?.Id, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Failed to find TreeBuilder for DataPort:{dataPort.Name} with ruleset id {dataPort.RulesetId}.");

            var rootNode = AddOrGetTreeRootNode(treeBuilder);

            treeBuilder.CreateDataPortTree(dataPort, rootNode);
            SortChildren(rootNode, true);
        }
        catch (Exception ex)
        {
            CreateDataPortTreeFailed(_logger, ex, dataPort.Id);
        }
    }

    private void UpdateChildrenIconsRecursively(DataPortNodeModel dataPortNode)
    {
        foreach (var child in dataPortNode.Children)
            UpdateChildrenIconsRecursively(child);

        Builder.Notifications.NotifyNodeChanged(dataPortNode, ChangedNodeDetail.Icons);
    }
}
