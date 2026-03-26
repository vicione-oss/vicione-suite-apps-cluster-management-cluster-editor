using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.Localization;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Components;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.MonochromeIcons.Core.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions.Arguments;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Services;

internal sealed partial class TopologyTreeAdapter : TreeAdapter, IDisposable
{
    private IClusterBuilder? _clusterBuilder;
    private readonly ClusterBuilderEventBuffer _clusterBuilderEventBuffer;
    private readonly List<TopologyTreeViewModel> _clusterNodeGroups = [];
    private TopologyTreeViewModel? _currentlyEditedNodeModel;
    private readonly IClusterEditorManagementInternal _dataManagementService;

    public Action<ITreeNode, Action>? OnDeleteNodeUserConfirmationRequest { get; set; }

    public TopologyTreeAdapter(ClusterBuilderEventBuffer clusterBuilderEventBuffer, IClusterEditorManagementInternal dataManagementService)
    {
        _clusterBuilderEventBuffer = clusterBuilderEventBuffer;
        _dataManagementService = dataManagementService;

        _clusterBuilderEventBuffer.NodeGroupsAdded += OnNodeGroupsAdded;
        _clusterBuilderEventBuffer.NodeGroupsRemoved += OnNodeGroupsRemoved;
        _clusterBuilderEventBuffer.NodesAdded += OnNodesAdded;
        _clusterBuilderEventBuffer.NodesRemoved += OnNodesRemoved;
        _clusterBuilderEventBuffer.ApplicationsAdded += OnApplicationsAdded;
        _clusterBuilderEventBuffer.ApplicationsRemoved += OnApplicationsRemoved;
        _clusterBuilderEventBuffer.EngineHostsAdded += OnEngineHostsAdded;
        _clusterBuilderEventBuffer.EngineHostsRemoved += OnEngineHostsRemoved;
        _clusterBuilderEventBuffer.EnginesAdded += OnEnginesAdded;
        _clusterBuilderEventBuffer.EnginesRemoved += OnEnginesRemoved;
    }

    private void AddTopologyNode(NodeButton _, VisibleActionArguments e)
    {
        if (_clusterBuilder is null)
            return;

        if (e.Node is not TopologyTreeViewModel nodeModel)
            return;

        switch (nodeModel.DataItem)
        {
            case ClusterNodeGroup nodeGroup:
                _clusterBuilder.Editors.NodeGroup.AddNode(nodeGroup);
                break;

            case ClusterNode cNode:
                _clusterBuilder.Editors.Node.AddApplication(cNode, ClusterApplicationType.CoreOsStandalone);
                break;

            case ClusterApplication application:
                _clusterBuilder.Editors.Application.AddEngineHost(application);
                break;

            case EngineHost engineHost:
                var name = _clusterBuilder.Editors.Engine.GetUniqueName(engineHost, null, "New Engine");
                _clusterBuilder.Editors.EngineHost.AddEngine(engineHost, name, engineType: EngineDefaults.EngineType);
                break;
        }

        e.Builder.Notifications.NotifyChildrenChanged(nodeModel);
    }

    public void AddTopologyNodeGroup()
    {
        if (_clusterBuilder is null)
            return;

        // NodeGroups could contain NodeGroups but we skip this till November milestone
        _clusterBuilder.Editors.Cluster.AddNodeGroup();
    }

    internal void BuildClusterTree(IClusterBuilder builder)
    {
        _clusterNodeGroups.Clear();
        _clusterBuilder = builder;
        RebuildTree();

        var firstNodeGroup = _clusterNodeGroups.FirstOrDefault();
        if (firstNodeGroup is not null)
        {
            firstNodeGroup.IsManuallySelected = true;
            Builder.Selection.ChangeSelection(firstNodeGroup, true);
        }
    }

    private static bool CanHaveAdditionalChildren(TopologyTreeViewModel model)
    {
        if (model.DataItem is Cluster.Model.Engine)
            return false;

        return true;
    }

    public override bool CanSelectNode(ITreeNode node, IEnumerable<ITreeNode> currentSelection, bool willDeselectOthers)
    {
        if (node is not TopologyTreeViewModel model)
            return false;

        if (willDeselectOthers)
            return true;

        return model.IsManuallySelected;
    }

    private void DeleteTopologyNode(NodeButton _, VisibleActionArguments e)
    {
        if (_clusterBuilder is null)
            return;

        if (e.Node is not TopologyTreeViewModel nodeModel)
            return;

        if (_currentlyEditedNodeModel is not null && _currentlyEditedNodeModel.Id.AsString == nodeModel.Id.AsString)
            _currentlyEditedNodeModel = null;

        void DeleteAction()
        {
            switch (nodeModel.DataItem)
            {
                case ClusterNodeGroup nodeGroup:
                    _clusterBuilder.Editors.Cluster.RemoveNodeGroup(nodeGroup);
                    break;

                case ClusterNode node:
                    _clusterBuilder.Editors.NodeGroup.RemoveNode(node);
                    break;

                case ClusterApplication application:
                    _clusterBuilder.Editors.Node.RemoveApplication(application);
                    break;

                case EngineHost engineHost:
                    _clusterBuilder.Editors.Application.RemoveEngineHost(engineHost);
                    break;

                case Cluster.Model.Engine engine:
                    _clusterBuilder.Editors.EngineHost.RemoveEngine(engine);
                    break;
            }
        }

        if (OnDeleteNodeUserConfirmationRequest is null)
            DeleteAction();
        else
            OnDeleteNodeUserConfirmationRequest(e.Node, DeleteAction);
    }

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;
    }

    public void Dispose()
    {
        _clusterBuilderEventBuffer.NodeGroupsAdded -= OnNodeGroupsAdded;
        _clusterBuilderEventBuffer.NodeGroupsRemoved -= OnNodeGroupsRemoved;
        _clusterBuilderEventBuffer.NodesAdded -= OnNodesAdded;
        _clusterBuilderEventBuffer.NodesRemoved -= OnNodesRemoved;
        _clusterBuilderEventBuffer.ApplicationsAdded -= OnApplicationsAdded;
        _clusterBuilderEventBuffer.ApplicationsRemoved -= OnApplicationsRemoved;
        _clusterBuilderEventBuffer.EngineHostsAdded -= OnEngineHostsAdded;
        _clusterBuilderEventBuffer.EngineHostsRemoved -= OnEngineHostsRemoved;
        _clusterBuilderEventBuffer.EnginesAdded -= OnEnginesAdded;
        _clusterBuilderEventBuffer.EnginesRemoved -= OnEnginesRemoved;
    }

    private async void EditTopologyNode(NodeButton _, VisibleActionArguments e)
    {
        if (e.Node is not TopologyTreeViewModel topologyNodeModel)
            return;

        if (_currentlyEditedNodeModel is not null && _currentlyEditedNodeModel.IsEditModeActive)
        {
            if (_currentlyEditedNodeModel.HasChangedProperties)
            {
                await _dataManagementService.ShowMessageToast(LogLevel.Warning, SharedSectionText.UnsavedNodeChanges, () => _currentlyEditedNodeModel.ScrollToNode(Builder));
                return;
            }

            _currentlyEditedNodeModel.IsEditModeActive = false;
            Builder.Notifications.NotifyNodeChanged(_currentlyEditedNodeModel, ChangedNodeDetail.None);
        }

        if (topologyNodeModel.Id != _currentlyEditedNodeModel?.Id || !_currentlyEditedNodeModel.IsEditModeActive)
        {
            topologyNodeModel.IsEditModeActive = true;
            topologyNodeModel.HasChangedProperties = false;
        }

        _currentlyEditedNodeModel = topologyNodeModel;
        Builder.Notifications.NotifyNodeChanged(topologyNodeModel, ChangedNodeDetail.None);
    }

    public void FilterNodes(string filterText)
        => TreeAdapterHelper.FilterNodesByDisplayText(Builder, (node) => ((TopologyTreeViewModel)node).Parent, filterText);

    public override IEnumerable<INodeAction> GetActions(ITreeNode node)
    {
        if (node is TopologyTreeViewModel model)
        {
            if (CanHaveAdditionalChildren(model))
            {
                yield return new NodeButton()
                {
                    Action = AddTopologyNode,
                    Description = "Add child",
                    EnabledFunc = (_) => true,
                    Icon = new TreeEditorMonochromeIcon(MonochromeIconName.PlusSlim, MonochromeIconSize.Small),
                };
            }

            yield return new NodeButton()
            {
                Action = EditTopologyNode,
                Description = "Edit",
                EnabledFunc = (node) => !((TopologyTreeViewModel)node).IsEditModeActive,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Edit, MonochromeIconSize.Small),
            };

            yield return new NodeButton()
            {
                Action = DeleteTopologyNode,
                Description = "Delete",
                EnabledFunc = (_) => true,
                Icon = new TreeEditorMonochromeIcon(MonochromeIconName.Delete, MonochromeIconSize.Small),
            };
        }
    }

    public override IEnumerable<ITreeNode> GetChildren(ITreeNode node)
    {
        if (node is not TopologyTreeViewModel model)
            return [];

        return model.Children;
    }

    public override string GetDisplayText(ITreeNode node)
    {
        if (node is TopologyTreeViewModel model)
            return model.DisplayText;

        return string.Empty;
    }

    public override IEnumerable<IIcon> GetIcons(ITreeNode node)
    {
        if (node is TopologyTreeViewModel { DataItem: var dataItem })
            yield return new TreeEditorMonochromeIcon(GetMonochromeIconName(dataItem), MonochromeIconSize.SmallMedium);
    }

    private static MonochromeIconName GetMonochromeIconName(object dataItem)
        => dataItem switch
        {
            ClusterNodeGroup => MonochromeIconName.TopologyFolderNetworkLight,
            ClusterNode => MonochromeIconName.ServerNetworkLight,
            ClusterApplication => MonochromeIconName.TopologyApplicationLight,
            EngineHost => MonochromeIconName.ServerLight,
            Cluster.Model.Engine => MonochromeIconName.TopologyEngineLight,
            _ => throw new InvalidOperationException($"Unknown topology data item type {dataItem.GetType()}"),
        };

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is not TopologyTreeViewModel model)
            return null;

        return model.Parent;
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => _clusterNodeGroups;

    private static Type? GetTemplateMapping(ITreeNode node, TemplateType templateType)
    {
        if (templateType == TemplateType.NodeDisplay)
        {
            if (node is TopologyTreeViewModel topologyNode && topologyNode.IsEditModeActive)
                return typeof(TopologyNodeEditTemplate);
        }

        return null;
    }

    public override bool HasChildren(ITreeNode node)
    {
        if (node is TopologyTreeViewModel model)
            return model.Children.Count != 0;

        return false;
    }

    public override bool IsExpanded(ITreeNode treeNode)
    {
        if (treeNode is TopologyTreeViewModel tModel)
            return tModel.Expanded;

        return false;
    }

    public override bool IsSelected(ITreeNode treeNode)
    {
        if (treeNode is TopologyTreeViewModel tModel)
            return tModel.Selected;

        return false;
    }

    private void OnExpansionChanged(ITreeNode node, bool expanded)
    {
        if (node is TopologyTreeViewModel tModel)
            tModel.Expanded = expanded;
    }

    private void OnSelectionChanged(ITreeNode node, bool selected)
    {
        if (node is TopologyTreeViewModel tModel)
            tModel.Selected = selected;
    }

    public void ProcessNodeChange(TopologyTreeViewModel model, object editItem)
    {
        if (_clusterBuilder is null)
            return;

        // edit item is a shallow copy of the orginal DataItem that contains the changes
        switch (editItem)
        {
            case ClusterNodeGroup nodeGroup:
                var originalNodeGroup = (ClusterNodeGroup)model.DataItem;
                originalNodeGroup.Apply(_clusterBuilder.Editors.NodeGroup, nodeGroup);
                model.DisplayText = originalNodeGroup.Name;
                break;

            case ClusterNode node:
                var originalNode = (ClusterNode)model.DataItem;
                originalNode.Apply(_clusterBuilder.Editors.Node, node);
                model.DisplayText = originalNode.Name;
                break;

            case ClusterApplication application:
                var originalApp = (ClusterApplication)model.DataItem;
                originalApp.Apply(_clusterBuilder.Editors.Application, application);
                model.DisplayText = originalApp.Name;
                break;

            case EngineHost engineHost:
                var originalHost = (EngineHost)model.DataItem;
                originalHost.Apply(_clusterBuilder.Editors.EngineHost, engineHost);
                model.DisplayText = originalHost.Name;
                break;

            case Cluster.Model.Engine engine:
                var originalEngine = (Cluster.Model.Engine)model.DataItem;
                originalEngine.Apply(_clusterBuilder.Editors.Engine, engine);
                model.DisplayText = originalEngine.Name;
                break;
        }

        Builder.Notifications.NotifyNodeChanged(model);
    }

    private void RebuildTree()
    {
        if (_clusterBuilder is null)
            return;

        _clusterNodeGroups.Clear();
        _clusterNodeGroups.AddRange(_clusterBuilder.Cluster.NodeGroups.Select(TopologyTreeFactory.BuildTreeFromClusterNodeGroup));

        Builder.Reset();
        Builder.Filter.Apply();
    }

    public override void Setup()
    {
        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
        Builder.Selection.SelectionChanged += OnSelectionChanged;

        Builder.Guidelines.Show = true;
        Builder.Template.Mapping = GetTemplateMapping;

        RebuildTree();
    }
}
