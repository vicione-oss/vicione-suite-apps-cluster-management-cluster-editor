using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(Cluster.Model.Cluster Parent, Dataflow Dataflow)> _dataflowAddedBuffer = [];
    private readonly List<(object? s, System.ComponentModel.PropertyChangedEventArgs e)> _dataflowPropertyChangedBuffer = [];
    private readonly List<(Cluster.Model.Cluster Parent, Dataflow Dataflow)> _dataflowRemovedBuffer = [];

    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? DataflowPropertiesChanged;
    public event Action<IEnumerable<(Cluster.Model.Cluster Parent, Dataflow Dataflow)>>? DataflowsAdded;
    public event Action<IEnumerable<(Cluster.Model.Cluster Parent, Dataflow Dataflow)>>? DataflowsRemoved;

    private void AttachDataflowEvents()
    {
        Builder.Editors.Cluster.DataflowAdded += OnDataflowAdded;
        Builder.Editors.Cluster.DataflowRemoved += OnDataflowRemoved;
        Builder.Editors.Dataflow.PropertyChanged += OnDataflowPropertyChanged;
    }

    private void DetachDataflowEvents()
    {
        Builder.Editors.Cluster.DataflowAdded -= OnDataflowAdded;
        Builder.Editors.Cluster.DataflowRemoved -= OnDataflowRemoved;
        Builder.Editors.Dataflow.PropertyChanged -= OnDataflowPropertyChanged;
    }

    private void FireDataflowEvents()
    {
        if (_dataflowAddedBuffer.Count > 0)
        {
            DataflowsAdded?.Invoke(_dataflowAddedBuffer);
            _dataflowAddedBuffer.Clear();
        }

        if (_dataflowPropertyChangedBuffer.Count > 0)
        {
            DataflowPropertiesChanged?.Invoke(_dataflowPropertyChangedBuffer);
            _dataflowPropertyChangedBuffer.Clear();
        }

        if (_dataflowRemovedBuffer.Count > 0)
        {
            DataflowsRemoved?.Invoke(_dataflowRemovedBuffer);
            _dataflowRemovedBuffer.Clear();
        }
    }

    private void OnDataflowAdded(object? sender, Dataflow dataflow)
    {
        if (sender is null)
            return;

        foreach (var df in _dataflowAddedBuffer)
        {
            if (df.Dataflow == dataflow)
                return;
        }

        _dataflowAddedBuffer.Add(new((Cluster.Model.Cluster)sender, dataflow));
        ScheduleBufferFlush();
    }

    public void OnDataflowPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _dataflowPropertyChangedBuffer.Add((s, e));
        ScheduleBufferFlush();
    }

    private void OnDataflowRemoved(object? sender, Dataflow dataflow)
    {
        if (sender is null)
            return;

        foreach (var df in _dataflowRemovedBuffer)
        {
            if (df.Dataflow == dataflow)
                return;
        }

        _dataflowRemovedBuffer.Add(new((Cluster.Model.Cluster)sender, dataflow));
        ScheduleBufferFlush();
    }
}
