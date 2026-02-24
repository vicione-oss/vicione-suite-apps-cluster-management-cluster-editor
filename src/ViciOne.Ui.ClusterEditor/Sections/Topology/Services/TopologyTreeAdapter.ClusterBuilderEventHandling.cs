using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Services;

internal sealed partial class TopologyTreeAdapter
{
    private void OnApplicationsAdded(IEnumerable<(ClusterNode Parent, ClusterApplication Application)> enumerable)
    {
        foreach (var (parent, application) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var applicationParent))
                continue;

            applicationParent.Children.Add(TopologyTreeFactory.CreateApplicationNode(application, applicationParent));
            Builder.Notifications.NotifyChildrenChanged(applicationParent);
        }
    }

    private void OnApplicationsRemoved(IEnumerable<(ClusterNode Parent, ClusterApplication Application)> enumerable)
    {
        foreach (var (parent, application) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var applicationParent))
                continue;

            applicationParent.Children.RemoveAll(n => n.DataItem == application);
            Builder.Notifications.NotifyChildrenChanged(applicationParent);
        }
    }

    private void OnEngineHostsAdded(IEnumerable<(ClusterApplication Parent, EngineHost EngineHost)> enumerable)
    {
        foreach (var (parent, engineHost) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var engineHostParent))
                continue;

            engineHostParent.Children.Add(TopologyTreeFactory.CreateEngineHostNode(engineHost, engineHostParent));
            Builder.Notifications.NotifyChildrenChanged(engineHostParent);
        }
    }

    private void OnEngineHostsRemoved(IEnumerable<(ClusterApplication Parent, EngineHost EngineHost)> enumerable)
    {
        foreach (var (parent, engineHost) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var engineHostParent))
                continue;

            engineHostParent.Children.RemoveAll(n => n.DataItem == engineHost);
            Builder.Notifications.NotifyChildrenChanged(engineHostParent);
        }
    }

    private void OnEnginesAdded(IEnumerable<(EngineHost Parent, Cluster.Model.Engine Engine)> enumerable)
    {
        foreach (var (parent, engine) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var engineParent))
                continue;

            engineParent.Children.Add(TopologyTreeFactory.CreateEngineNode(engine, engineParent));
            Builder.Notifications.NotifyChildrenChanged(engineParent);
        }
    }

    private void OnEnginesRemoved(IEnumerable<(EngineHost Parent, Cluster.Model.Engine Engine)> enumerable)
    {
        foreach (var (parent, engine) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var engineParent))
                continue;

            engineParent.Children.RemoveAll(n => n.DataItem == engine);
            Builder.Notifications.NotifyChildrenChanged(engineParent);
        }
    }

    private void OnNodeGroupsAdded(IEnumerable<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)> enumerable)
    {
        foreach (var (parent, nodeGroup) in enumerable)
        {
            TopologyTreeViewModel? nodeGroupParent = null;
            if (parent is ClusterNodeGroup && !TryFindParent(tm => tm.DataItem == parent, out nodeGroupParent))
                continue;

            if (nodeGroupParent is null)
            {
                _clusterNodeGroups.Add(TopologyTreeFactory.CreateClusterNodeGroupNode(nodeGroup, nodeGroupParent));
                Builder.Notifications.NotifyRootNodesChanged();
            }
            else
            {
                nodeGroupParent.Children.Add(TopologyTreeFactory.CreateClusterNodeGroupNode(nodeGroup, nodeGroupParent));
                Builder.Notifications.NotifyChildrenChanged(nodeGroupParent);
            }
        }
    }

    private void OnNodeGroupsRemoved(IEnumerable<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)> enumerable)
    {
        foreach (var (parent, nodeGroup) in enumerable)
        {
            TopologyTreeViewModel? nodeGroupParent = null;
            if (parent is ClusterNodeGroup && !TryFindParent(tm => tm.DataItem == parent, out nodeGroupParent))
                continue;

            if (nodeGroupParent is null)
            {
                _clusterNodeGroups.RemoveAll(n => n.DataItem == nodeGroup);
                Builder.Notifications.NotifyRootNodesChanged();
            }
            else
            {
                nodeGroupParent.Children.RemoveAll(n => n.DataItem == nodeGroup);
                Builder.Notifications.NotifyChildrenChanged(nodeGroupParent);
            }
        }
    }

    private void OnNodesAdded(IEnumerable<(ClusterNodeGroup Parent, ClusterNode Node)> enumerable)
    {
        foreach (var (parent, node) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var nodeParent))
                continue;

            nodeParent.Children.Add(TopologyTreeFactory.CreateClusterNodeNode(node, nodeParent));
            Builder.Notifications.NotifyChildrenChanged(nodeParent);
        }
    }

    private void OnNodesRemoved(IEnumerable<(ClusterNodeGroup Parent, ClusterNode Node)> enumerable)
    {
        foreach (var (parent, node) in enumerable)
        {
            if (!TryFindParent(tm => tm.DataItem == parent, out var nodeParent))
                continue;

            nodeParent.Children.RemoveAll(n => n.DataItem == node);
            Builder.Notifications.NotifyChildrenChanged(nodeParent);
        }
    }

    private bool TryFindParent(Predicate<TopologyTreeViewModel> selector, [NotNullWhen(true)] out TopologyTreeViewModel? parent)
    {
        foreach (var rootNode in _clusterNodeGroups)
        {
            if (selector(rootNode))
            {
                parent = rootNode;
                return true;
            }

            if (TryFindParentRecursive(rootNode, out parent))
                return true;
        }

        parent = null;
        return false;

        bool TryFindParentRecursive(TopologyTreeViewModel node, [NotNullWhen(true)] out TopologyTreeViewModel? parent)
        {
            foreach (var child in node.Children)
            {
                if (selector(child))
                {
                    parent = child;
                    return true;
                }

                if (TryFindParentRecursive(child, out parent))
                    return true;
            }

            parent = null;
            return false;
        }
    }
}
