using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder.Interface;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;
using ViciOne.Ui.TreeEditor.Builder.Interface.Icons;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeActions;
using ViciOne.Ui.TreeEditor.Builder.Interface.Nodes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortTreeAdapter(
    DataPortNodeActionProvider actionProvider,
    IDatastore datastore,
    DataPortTreeState treeState,
    DataPortDragCoordinator dragCoordinator,
    DataPortClusterEventSynchronizer eventSynchronizer,
    DataPortIconResolver iconResolver,
    ILogger<DataPortTreeAdapter> logger,
    DataPortTreeMutator mutator,
    DataPortTreeBuilderRegistry treeBuilderRegistry) : TreeAdapter, IDisposable
{
    public const string DataPortCategory = DataPortTreeBuilderRegistry.DataPortCategory;

    public Action<ITreeNode, Action>? OnDeleteNodeUserConfirmationRequest
    {
        get => treeState.OnDeleteNodeUserConfirmationRequest;
        set => treeState.OnDeleteNodeUserConfirmationRequest = value;
    }
    public List<ITreeNode> ValidInboundDropTargets => treeState.ValidInboundDropTargets;

    public event Func<ITreeNode, Task>? DataPortWithLinksDoubleClicked;

    public override bool CanDragNode(ITreeNode treeNode)
        => treeNode is not DataPortChildNodeModel childNode || !childNode.IsLockedByEditMode();

    public override bool CanInboundDropAsChild(ITreeNode target)
        => ValidInboundDropTargets.Contains(target);

    public void CreateNewChildNode(ITreeNode parentNode, ITreeNode newChild)
        => mutator.CreateNewChildNode(parentNode, newChild);

    public void CreateNewDataPortRootNode(string rulesetIdentifier)
        => mutator.CreateNewDataPortRootNode(rulesetIdentifier);

    public override void Dismantle()
    {
        Builder.Expansion.ExpansionChanged -= OnExpansionChanged;
        Builder.Selection.SelectionChanged -= OnSelectionChanged;

        dragCoordinator.Detach(Builder);
    }

    public void Dispose()
        => eventSynchronizer.Dispose();

    public void FilterNodes(string filterText)
    {
        TreeAdapterHelper.FilterNodesByDisplayText(Builder, ResolveParent, filterText);

        static ITreeNode? ResolveParent(ITreeNode node)
        {
            if (node is not DataPortChildNodeModel childNode)
                return null;

            return childNode.Parent;
        }
    }

    public override IEnumerable<INodeAction> GetActions(ITreeNode node)
        => actionProvider.GetActions(node);

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
        => treeState.GetDataPortRootNode(rulesetRootId);

    public override Action<ITreeNode>? GetDblClickAction(ITreeNode node)
        => n =>
        {
            if (n is not DataPortNodeModel dpNode)
                return;

            if (datastore.Builder.Cache.DataPortTreeNodeIds.TryGetValue(dpNode.Id.Value, out var treeNode) && treeNode.Links.Any())
                DataPortWithLinksDoubleClicked?.InvokeEventAsync(node, logger, nameof(DataPortWithLinksDoubleClicked));
            else
                Builder.Expansion.ChangeExpansion(node, !((DataPortNodeModel)node).Expanded);
        };

    public override string GetDisplayText(ITreeNode node)
    {
        if (node is DataPortNodeModel dpNode)
            return dpNode.Name;

        return string.Empty;
    }

    public override IEnumerable<IIcon> GetIcons(ITreeNode node)
        => iconResolver.GetIcons(node);

    public override ITreeNode? GetParent(ITreeNode node)
    {
        if (node is not DataPortChildNodeModel dataPortChildNode)
            return null;

        return dataPortChildNode.Parent;
    }

    public override IEnumerable<ITreeNode> GetRootNodes()
        => treeState.RootNodes;

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
        => treeState.GetTreeNode(dataPortTreeNode);

    public override bool HasChildren(ITreeNode node)
    {
        if (node is not DataPortNodeModel dataportNode)
            return false;

        return dataportNode.Children.Count != 0;
    }

    public void Initialize()
    {
        eventSynchronizer.Initialize();
        treeBuilderRegistry.Initialize();
    }

    public void InitializeDataPortTree()
        => mutator.InitializeDataPortTree();

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

    public void NotifyDescendantsChanged(DataPortNodeModel node)
        => node.NotifyDescendantsChanged(Builder);

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

    public void ProcessNodeChanges(ITreeNode node)
        => mutator.ProcessNodeChanges(node);

    public void RevertNodeChanges(DataPortChildNodeModel childNode)
        => mutator.RevertNodeChanges(childNode);

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
        treeState.Builder = Builder;

        Builder.Expansion.ExpansionChanged += OnExpansionChanged;
        Builder.Selection.SelectionChanged += OnSelectionChanged;

        dragCoordinator.Attach(Builder);

        Builder.Guidelines.Show = true;
        Builder.Template.Mapping = GetTemplateMapping;
    }

    internal void SortNodeChildren(DataPortNodeModel node)
        => DataPortNodeSorter.SortNodeChildren(node, Builder);
}
