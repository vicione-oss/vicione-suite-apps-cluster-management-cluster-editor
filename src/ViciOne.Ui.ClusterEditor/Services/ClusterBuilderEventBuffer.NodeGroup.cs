using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)> _nodeGroupAddedBuffer = [];
    private readonly List<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)> _nodeGroupRemovedBuffer = [];

    public event Action<IEnumerable<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)>>? NodeGroupsAdded;
    public event Action<IEnumerable<(IHasClusterNodeGroups? Parent, ClusterNodeGroup NodeGroup)>>? NodeGroupsRemoved;

    private void AttachNodeGroupEvents()
    {
        Builder.Editors.Cluster.NodeGroupAdded += OnNodeGroupAdded;
        Builder.Editors.Cluster.NodeGroupRemoved += OnNodeGroupRemoved;
    }

    private void DetachNodeGroupEvents()
    {
        Builder.Editors.Cluster.NodeGroupAdded -= OnNodeGroupAdded;
        Builder.Editors.Cluster.NodeGroupRemoved -= OnNodeGroupRemoved;
    }

    private void FireNodeGroupEvents()
    {
        if (_nodeGroupAddedBuffer.Count > 0)
        {
            NodeGroupsAdded?.Invoke([.. _nodeGroupAddedBuffer]);
            _nodeGroupAddedBuffer.Clear();
        }

        if (_nodeGroupRemovedBuffer.Count > 0)
        {
            NodeGroupsRemoved?.Invoke([.. _nodeGroupRemovedBuffer]);
            _nodeGroupRemovedBuffer.Clear();
        }
    }

    private void OnNodeGroupAdded(object? sender, ClusterNodeGroup nodeGroup)
    {
        if (_nodeGroupAddedBuffer.Any(ng => ng.NodeGroup == nodeGroup))
            return;

        _nodeGroupAddedBuffer.Add(new(sender is null ? null : (IHasClusterNodeGroups)sender, nodeGroup));
        StartBufferTimer();
    }

    private void OnNodeGroupRemoved(object? sender, ClusterNodeGroup nodeGroup)
    {
        if (_nodeGroupRemovedBuffer.Any(ng => ng.NodeGroup == nodeGroup))
            return;

        _nodeGroupRemovedBuffer.Add(new(sender is null ? null : (IHasClusterNodeGroups)sender, nodeGroup));
        StartBufferTimer();
    }
}
