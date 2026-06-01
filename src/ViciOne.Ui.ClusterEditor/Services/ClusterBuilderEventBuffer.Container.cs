using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(Container Parent, Container Container)> _containerAddedBuffer = [];
    private readonly List<Container> _containerChangedBuffer = [];
    private readonly List<(object? s, System.ComponentModel.PropertyChangedEventArgs e)> _containerPropertyChangedBuffer = [];
    private readonly List<(Container Parent, Container Container)> _containerRemovedBuffer = [];
    private readonly List<(Container Parent, Label Label)> _labelAddedBuffer = [];
    private readonly List<(Container Parent, Label Label)> _labelRemovedBuffer = [];

    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? ContainerPropertiesChanged;
    public event Action<IEnumerable<(Container Parent, Container Container)>>? ContainersAdded;
    public event Action<IEnumerable<Container>>? ContainersChanged;
    public event Action<IEnumerable<(Container Parent, Container Container)>>? ContainersRemoved;
    public event Action<IEnumerable<(Container Parent, Label Label)>>? LabelsAdded;
    public event Action<IEnumerable<(Container Parent, Label Label)>>? LabelsRemoved;

    private void AttachContainerEvents()
    {
        Builder.Editors.Container.ContainerAdded += OnContainerAdded;
        Builder.Editors.Container.ContainerRemoved += OnContainerRemoved;
        Builder.Editors.Container.ContainerChanged += OnContainerChanged;
        Builder.Editors.Container.LabelAdded += OnLabelAdded;
        Builder.Editors.Container.LabelRemoved += OnLabelRemoved;
        Builder.Editors.Container.PropertyChanged += OnContainerPropertyChanged;
    }

    private void DetachContainerEvents()
    {
        Builder.Editors.Container.ContainerAdded -= OnContainerAdded;
        Builder.Editors.Container.ContainerRemoved -= OnContainerRemoved;
        Builder.Editors.Container.ContainerChanged -= OnContainerChanged;
        Builder.Editors.Container.LabelAdded -= OnLabelAdded;
        Builder.Editors.Container.LabelRemoved -= OnLabelRemoved;
        Builder.Editors.Container.PropertyChanged -= OnContainerPropertyChanged;
    }

    private void FireContainerEvents()
    {
        if (_containerAddedBuffer.Count > 0)
        {
            ContainersAdded?.Invoke([.. _containerAddedBuffer]);
            _containerAddedBuffer.Clear();
        }

        if (_containerChangedBuffer.Count > 0)
        {
            ContainersChanged?.Invoke([.. _containerChangedBuffer]);
            _containerChangedBuffer.Clear();
        }

        if (_containerPropertyChangedBuffer.Count > 0)
        {
            ContainerPropertiesChanged?.Invoke([.. _containerPropertyChangedBuffer]);
            _containerPropertyChangedBuffer.Clear();
        }

        if (_containerRemovedBuffer.Count > 0)
        {
            ContainersRemoved?.Invoke([.. _containerRemovedBuffer]);
            _containerRemovedBuffer.Clear();
        }

        if (_labelAddedBuffer.Count > 0)
        {
            LabelsAdded?.Invoke([.. _labelAddedBuffer]);
            _labelAddedBuffer.Clear();
        }

        if (_labelRemovedBuffer.Count > 0)
        {
            LabelsRemoved?.Invoke([.. _labelRemovedBuffer]);
            _labelRemovedBuffer.Clear();
        }
    }

    private void OnContainerAdded(object? sender, Container container)
    {
        if (_containerAddedBuffer.Any(c => c.Container == container) || sender is null)
            return;

        _containerAddedBuffer.Add(new((Container)sender, container));
        ScheduleBufferFlush();
    }

    private void OnContainerChanged(Container container)
    {
        if (_containerChangedBuffer.Contains(container))
            return;

        _containerChangedBuffer.Add(container);
        ScheduleBufferFlush();
    }

    private void OnContainerPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _containerPropertyChangedBuffer.Add((s, e));
        ScheduleBufferFlush();
    }

    private void OnContainerRemoved(object? sender, Container container)
    {
        if (_containerRemovedBuffer.Any(c => c.Container == container) || sender is null)
            return;

        _containerRemovedBuffer.Add(new((Container)sender, container));
        ScheduleBufferFlush();
    }

    private void OnLabelAdded(object? sender, Label label)
    {
        if (_labelAddedBuffer.Any(l => l.Label == label) || sender is null)
            return;

        _labelAddedBuffer.Add(new((Container)sender, label));
        ScheduleBufferFlush();
    }

    private void OnLabelRemoved(object? sender, Label label)
    {
        if (_labelRemovedBuffer.Any(l => l.Label == label) || sender is null)
            return;

        _labelRemovedBuffer.Add(new((Container)sender, label));
        ScheduleBufferFlush();
    }
}
