using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(ClusterNode Parent, ClusterApplication Application)> _applicationAddedBuffer = [];
    private readonly List<(ClusterNode Parent, ClusterApplication Application)> _applicationRemovedBuffer = [];
    private readonly List<(ClusterNodeGroup Parent, ClusterNode Node)> _nodeAddedBuffer = [];
    private readonly List<(ClusterNodeGroup Parent, ClusterNode Node)> _nodeRemovedBuffer = [];

    public event Action<IEnumerable<(ClusterNode Parent, ClusterApplication Application)>>? ApplicationsAdded;
    public event Action<IEnumerable<(ClusterNode Parent, ClusterApplication Application)>>? ApplicationsRemoved;
    public event Action<IEnumerable<(ClusterNodeGroup Parent, ClusterNode Node)>>? NodesAdded;
    public event Action<IEnumerable<(ClusterNodeGroup Parent, ClusterNode Node)>>? NodesRemoved;

    private void AttachNodeEvents()
    {
        Builder.Editors.Node.ApplicationAdded += OnApplicationAdded;
        Builder.Editors.Node.ApplicationRemoved += OnApplicationRemoved;
        Builder.Editors.NodeGroup.NodeAdded += OnNodeAdded;
        Builder.Editors.NodeGroup.NodeRemoved += OnNodeRemoved;
    }

    private void DetachNodeEvents()
    {
        Builder.Editors.Node.ApplicationAdded -= OnApplicationAdded;
        Builder.Editors.Node.ApplicationRemoved -= OnApplicationRemoved;
        Builder.Editors.NodeGroup.NodeAdded -= OnNodeAdded;
        Builder.Editors.NodeGroup.NodeRemoved -= OnNodeRemoved;
    }

    private void FireNodeEvents()
    {
        if (_applicationAddedBuffer.Count > 0)
        {
            ApplicationsAdded?.Invoke([.. _applicationAddedBuffer]);
            _applicationAddedBuffer.Clear();
        }

        if (_applicationRemovedBuffer.Count > 0)
        {
            ApplicationsRemoved?.Invoke([.. _applicationRemovedBuffer]);
            _applicationRemovedBuffer.Clear();
        }

        if (_nodeAddedBuffer.Count > 0)
        {
            NodesAdded?.Invoke([.. _nodeAddedBuffer]);
            _nodeAddedBuffer.Clear();
        }

        if (_nodeRemovedBuffer.Count > 0)
        {
            NodesRemoved?.Invoke([.. _nodeRemovedBuffer]);
            _nodeRemovedBuffer.Clear();
        }
    }

    private void OnApplicationAdded(object? sender, ClusterApplication application)
    {
        if (_applicationAddedBuffer.Any(app => app.Application == application) || sender is null)
            return;

        _applicationAddedBuffer.Add(new((ClusterNode)sender, application));
        ScheduleBufferFlush();
    }

    private void OnApplicationRemoved(object? sender, ClusterApplication application)
    {
        if (_applicationAddedBuffer.Any(app => app.Application == application) || sender is null)
            return;

        _applicationRemovedBuffer.Add(new((ClusterNode)sender, application));
        ScheduleBufferFlush();
    }

    private void OnNodeAdded(object? sender, ClusterNode node)
    {
        if (_nodeAddedBuffer.Any(n => n.Node == node) || sender is null)
            return;

        _nodeAddedBuffer.Add(new((ClusterNodeGroup)sender, node));
        ScheduleBufferFlush();
    }

    private void OnNodeRemoved(object? sender, ClusterNode node)
    {
        if (_nodeRemovedBuffer.Any(n => n.Node == node) || sender is null)
            return;

        _nodeRemovedBuffer.Add(new((ClusterNodeGroup)sender, node));
        ScheduleBufferFlush();
    }
}
