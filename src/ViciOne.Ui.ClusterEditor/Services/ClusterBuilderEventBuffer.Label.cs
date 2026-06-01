using System;
using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(object? sender, System.ComponentModel.PropertyChangedEventArgs e)> _labelPropertyChangedBuffer = [];

    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? LabelPropertiesChanged;

    private void AttachLabelEvents()
        => Builder.Editors.Label.PropertyChanged += OnLabelPropertyChanged;

    private void DetachLabelEvents()
        => Builder.Editors.Label.PropertyChanged -= OnLabelPropertyChanged;

    private void FireLabelEvents()
    {
        if (_labelPropertyChangedBuffer.Count > 0)
        {
            LabelPropertiesChanged?.Invoke([.. _labelPropertyChangedBuffer]);
            _labelPropertyChangedBuffer.Clear();
        }
    }

    private void OnLabelPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _labelPropertyChangedBuffer.Add((s, e));
        ScheduleBufferFlush();
    }
}
