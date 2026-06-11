using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(Dataflow Parent, DataPort DataPort)> _dataPortAddedBuffer = [];
    private readonly List<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> _dataPortPropertyChangedBuffer = [];
    private readonly List<(Dataflow Parent, DataPort DataPort)> _dataPortRemovedBuffer = [];
    private readonly List<Link> _dataPortTreeNodeLinkAddedBuffer = [];
    private readonly List<Link> _dataPortTreeNodeLinkRemovedBuffer = [];
    private readonly List<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)> _treeNodeAddedBuffer = [];
    private readonly List<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)> _treeNodeRemovedBuffer = [];

    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? DataPortPropertiesChanged;
    public event Action<IEnumerable<(Dataflow Parent, DataPort DataPort)>>? DataPortsAdded;
    public event Action<IEnumerable<(Dataflow Parent, DataPort DataPort)>>? DataPortsRemoved;
    public event Action<IEnumerable<Link>>? DataPortTreeNodeLinksAdded;
    public event Action<IEnumerable<Link>>? DataPortTreeNodeLinksRemoved;
    public event Action<IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)>>? TreeNodesAdded;
    public event Action<IEnumerable<(IHasDataPortTreeNodes Parent, DataPortTreeNode DataPortTreeNode)>>? TreeNodesRemoved;

    private void AttachDataPortEvents()
    {
        Builder.Editors.Dataflow.DataPortAdded += OnDataPortAdded;
        Builder.Editors.Dataflow.DataPortRemoved += OnDataPortRemoved;
        Builder.Editors.DataPort.PropertyChanged += OnDataPortPropertyChanged;
        Builder.Editors.DataPort.TreeNodeAdded += OnTreeNodeAdded;
        Builder.Editors.DataPort.TreeNodeRemoved += OnTreeNodeRemoved;
        Builder.Editors.DataPortTreeNode.LinkAdded += OnDataPortTreeNodeLinkAdded;
        Builder.Editors.DataPortTreeNode.LinkRemoved += OnDataPortTreeNodeLinkRemoved;
    }

    private void DetachDataPortEvents()
    {
        Builder.Editors.Dataflow.DataPortAdded -= OnDataPortAdded;
        Builder.Editors.Dataflow.DataPortRemoved -= OnDataPortRemoved;
        Builder.Editors.DataPort.PropertyChanged -= OnDataPortPropertyChanged;
        Builder.Editors.DataPort.TreeNodeAdded -= OnTreeNodeAdded;
        Builder.Editors.DataPort.TreeNodeRemoved -= OnTreeNodeRemoved;
        Builder.Editors.DataPortTreeNode.LinkAdded -= OnDataPortTreeNodeLinkAdded;
        Builder.Editors.DataPortTreeNode.LinkRemoved -= OnDataPortTreeNodeLinkRemoved;
    }

    private void FireDataPortEvents()
    {
        if (_dataPortAddedBuffer.Count > 0)
        {
            DataPortsAdded?.Invoke(_dataPortAddedBuffer);
            _dataPortAddedBuffer.Clear();
        }

        if (_dataPortRemovedBuffer.Count > 0)
        {
            DataPortsRemoved?.Invoke(_dataPortRemovedBuffer);
            _dataPortRemovedBuffer.Clear();
        }

        if (_dataPortPropertyChangedBuffer.Count > 0)
        {
            DataPortPropertiesChanged?.Invoke(_dataPortPropertyChangedBuffer);
            _dataPortPropertyChangedBuffer.Clear();
        }

        if (_dataPortTreeNodeLinkAddedBuffer.Count > 0)
        {
            DataPortTreeNodeLinksAdded?.Invoke(_dataPortTreeNodeLinkAddedBuffer);
            _dataPortTreeNodeLinkAddedBuffer.Clear();
        }

        if (_dataPortTreeNodeLinkRemovedBuffer.Count > 0)
        {
            DataPortTreeNodeLinksRemoved?.Invoke(_dataPortTreeNodeLinkRemovedBuffer);
            _dataPortTreeNodeLinkRemovedBuffer.Clear();
        }

        if (_treeNodeAddedBuffer.Count > 0)
        {
            TreeNodesAdded?.Invoke(_treeNodeAddedBuffer);
            _treeNodeAddedBuffer.Clear();
        }

        if (_treeNodeRemovedBuffer.Count > 0)
        {
            TreeNodesRemoved?.Invoke(_treeNodeRemovedBuffer);
            _treeNodeRemovedBuffer.Clear();
        }
    }

    private void OnDataPortAdded(object? sender, DataPort dataPort)
    {
        if (sender is null)
            return;

        foreach (var dp in _dataPortAddedBuffer)
        {
            if (dp.DataPort == dataPort)
                return;
        }

        _dataPortAddedBuffer.Add(new((Dataflow)sender, dataPort));
        ScheduleBufferFlush();
    }

    private void OnDataPortPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _dataPortPropertyChangedBuffer.Add((s, e));
        ScheduleBufferFlush();
    }

    private void OnDataPortRemoved(object? sender, DataPort dataPort)
    {
        if (sender is null)
            return;

        foreach (var dp in _dataPortRemovedBuffer)
        {
            if (dp.DataPort == dataPort)
                return;
        }

        _dataPortRemovedBuffer.Add(new((Dataflow)sender, dataPort));
        ScheduleBufferFlush();
    }

    private void OnDataPortTreeNodeLinkAdded(Link link)
    {
        if (_dataPortTreeNodeLinkAddedBuffer.Contains(link))
            return;

        _dataPortTreeNodeLinkAddedBuffer.Add(link);
        ScheduleBufferFlush();
    }

    private void OnDataPortTreeNodeLinkRemoved(Link link)
    {
        if (_dataPortTreeNodeLinkRemovedBuffer.Contains(link))
            return;

        _dataPortTreeNodeLinkRemovedBuffer.Add(link);
        ScheduleBufferFlush();
    }

    private void OnTreeNodeAdded(object? sender, DataPortTreeNode dataPortTreeNode)
    {
        if (sender is null)
            return;

        foreach (var dptn in _treeNodeAddedBuffer)
        {
            if (dptn.DataPortTreeNode == dataPortTreeNode)
                return;
        }

        _treeNodeAddedBuffer.Add(new((IHasDataPortTreeNodes)sender, dataPortTreeNode));
        ScheduleBufferFlush();
    }

    private void OnTreeNodeRemoved(object? sender, DataPortTreeNode dataPortTreeNode)
    {
        if (sender is null)
            return;

        foreach (var dptn in _treeNodeRemovedBuffer)
        {
            if (dptn.DataPortTreeNode == dataPortTreeNode)
                return;
        }

        _treeNodeRemovedBuffer.Add(new((IHasDataPortTreeNodes)sender, dataPortTreeNode));
        ScheduleBufferFlush();
    }
}
