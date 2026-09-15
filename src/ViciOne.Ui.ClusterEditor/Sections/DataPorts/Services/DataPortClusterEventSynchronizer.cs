using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.TreeEditor.Builder.Interface.Enums;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortClusterEventSynchronizer(
    ClusterBuilderEventBuffer clusterBuilderEventBuffer,
    IRulesetProvider rulesetProvider,
    IDatastore datastore,
    DataPortTreeState state) : IDisposable
{
    private const string DirectionPropertyName = "Direction";

    public void Dispose()
    {
        clusterBuilderEventBuffer.DataPortsRemoved -= OnDataPortsRemoved;
        clusterBuilderEventBuffer.DataPortPropertiesChanged -= OnDataPortPropertiesChanged;
        clusterBuilderEventBuffer.TreeNodesRemoved -= OnTreeNodesRemoved;
        clusterBuilderEventBuffer.DataPortTreeNodeLinksAdded -= OnDataPortTreeNodeLinksChanged;
        clusterBuilderEventBuffer.DataPortTreeNodeLinksRemoved -= OnDataPortTreeNodeLinksChanged;
    }

    public void Initialize()
    {
        clusterBuilderEventBuffer.DataPortsRemoved += OnDataPortsRemoved;
        clusterBuilderEventBuffer.DataPortPropertiesChanged += OnDataPortPropertiesChanged;
        clusterBuilderEventBuffer.TreeNodesRemoved += OnTreeNodesRemoved;
        clusterBuilderEventBuffer.DataPortTreeNodeLinksAdded += OnDataPortTreeNodeLinksChanged;
        clusterBuilderEventBuffer.DataPortTreeNodeLinksRemoved += OnDataPortTreeNodeLinksChanged;
    }

    private void OnDataPortPropertiesChanged(IEnumerable<(object? sender, PropertyChangedEventArgs e)> changedDataPortProperties)
    {
        if (state.IsDeletionInProgress)
            return;

        foreach (var (sender, e) in changedDataPortProperties)
        {
            if (e.PropertyName != DirectionPropertyName)
                continue;

            if (sender is not DataPort dataPort)
                continue;

            var dataPortNode = state.FindNode(dataPort.Id);

            if (dataPortNode is null)
                continue;

            // The icons of the children must change too. This should be done by a call to
            // Events.InvokeChildrenChanged(dataPortNode), but currently that method doesn't
            // update the icons of the children.
            dataPortNode.NotifyIconsChangedRecursively(state.Builder);
        }
    }

    private void OnDataPortsRemoved(IEnumerable<(Cluster.Model.Dataflow Parent, DataPort DataPort)> dataPorts)
    {
        foreach (var dataPort in dataPorts)
        {
            var dataPortNode = state.FindNode(dataPort.DataPort.Id);
            if (dataPortNode is null)
                continue;

            dataPortNode.RootNode.Children.Remove(dataPortNode);

            if (dataPortNode.RootNode.Children.Count != 0)
            {
                dataPortNode.RootNode.PossibleChildren = [.. dataPortNode.RootNode.GetPossibleChildNodes()];
                state.Builder.Notifications.NotifyNodeChanged(dataPortNode.RootNode, ChangedNodeDetail.Actions | ChangedNodeDetail.Icons);
                state.Builder.Notifications.NotifyChildrenChanged(dataPortNode.RootNode);
            }
            else
            {
                state.RemoveRootNode(dataPortNode.RootNode);
                datastore.Builder?.RemoveUnusedSystemDataPortDependency(rulesetProvider);
                state.Builder.Notifications.NotifyRootNodesChanged();
            }
        }

        ClearEditingNodeIfItIsGone();

        // A DataPort without tree nodes is removed without a tree node event following it, so this
        // is the last event of such a deletion.
        state.IsDeletionInProgress = false;
    }

    private void OnDataPortTreeNodeLinksChanged(IEnumerable<Link> links)
    {
        if (state.IsDeletionInProgress)
            return;

        foreach (var link in links)
        {
            DataPortChildNodeModel? childNode = null;

            if (link.SourceDataPortTreeNode is not null)
                childNode = state.FindNode(link.SourceDataPortTreeNode.Id);

            if (link.DestinationDataPortTreeNode is not null)
                childNode = state.FindNode(link.DestinationDataPortTreeNode.Id);

            if (childNode is null)
                continue;

            state.Builder.Notifications.NotifyNodeChanged(childNode, ChangedNodeDetail.Icons);
        }
    }

    private void OnTreeNodesRemoved(IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)> treeNodes)
    {
        foreach (var treeNode in treeNodes)
        {
            var childNode = state.FindNode(treeNode.DataPortTreeNode.Id);

            if (childNode is null)
                continue;

            if (childNode.Parent is not DataPortChildNodeModel parent)
                continue;

            parent.Children.Remove(childNode);
            parent.PossibleChildren = [.. parent.GetPossibleChildNodes()];
            state.Builder.Notifications.NotifyNodeChanged(parent, ChangedNodeDetail.Actions);
            state.Builder.Notifications.NotifyChildrenChanged(parent);
        }

        ClearEditingNodeIfItIsGone();
        state.IsDeletionInProgress = false;
    }

    /// <summary>
    /// Forgets the node being edited once the tree no longer holds it. The removal event does not
    /// always name it: deleting an ancestor takes the whole subtree along. A node left behind here
    /// would keep every later edit reporting unsaved changes for a node the user cannot see any
    /// more, and hand the toast a node the tree builder has already dropped.
    /// </summary>
    private void ClearEditingNodeIfItIsGone()
    {
        if (state.EditingTreeNode is { } editingNode && state.FindAnyNode(editingNode.Id) is null)
            state.EditingTreeNode = null;
    }
}
