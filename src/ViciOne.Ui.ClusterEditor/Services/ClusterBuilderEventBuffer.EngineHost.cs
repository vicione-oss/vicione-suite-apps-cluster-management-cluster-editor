using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<(EngineHost Parent, Cluster.Model.Engine Engine)> _engineAddedBuffer = [];
    private readonly List<(ClusterApplication Parent, EngineHost EngineHost)> _engineHostAddedBuffer = [];
    private readonly List<(ClusterApplication Parent, EngineHost EngineHost)> _engineHostRemovedBuffer = [];
    private readonly List<(EngineHost Parent, Cluster.Model.Engine Engine)> _engineRemovedBuffer = [];

    public event Action<IEnumerable<(ClusterApplication Parent, EngineHost EngineHost)>>? EngineHostsAdded;
    public event Action<IEnumerable<(ClusterApplication Parent, EngineHost EngineHost)>>? EngineHostsRemoved;
    public event Action<IEnumerable<(EngineHost Parent, Cluster.Model.Engine Engine)>>? EnginesAdded;
    public event Action<IEnumerable<(EngineHost Parent, Cluster.Model.Engine Engine)>>? EnginesRemoved;

    private void AttachEngineHostEvents()
    {
        Builder.Editors.EngineHost.EngineAdded += OnEngineAdded;
        Builder.Editors.Application.EngineHostAdded += OnEngineHostAdded;
        Builder.Editors.Application.EngineHostRemoved += OnEngineHostRemoved;
        Builder.Editors.EngineHost.EngineRemoved += OnEngineRemoved;
    }

    private void DetachEngineHostEvents()
    {
        Builder.Editors.EngineHost.EngineAdded -= OnEngineAdded;
        Builder.Editors.Application.EngineHostAdded -= OnEngineHostAdded;
        Builder.Editors.Application.EngineHostRemoved -= OnEngineHostRemoved;
        Builder.Editors.EngineHost.EngineRemoved -= OnEngineRemoved;
    }

    private void FireEngineHostEvents()
    {
        if (_engineAddedBuffer.Count > 0)
        {
            EnginesAdded?.Invoke(_engineAddedBuffer);
            _engineAddedBuffer.Clear();
        }

        if (_engineHostAddedBuffer.Count > 0)
        {
            EngineHostsAdded?.Invoke(_engineHostAddedBuffer);
            _engineHostAddedBuffer.Clear();
        }

        if (_engineHostRemovedBuffer.Count > 0)
        {
            EngineHostsRemoved?.Invoke(_engineHostRemovedBuffer);
            _engineHostRemovedBuffer.Clear();
        }

        if (_engineRemovedBuffer.Count > 0)
        {
            EnginesRemoved?.Invoke(_engineRemovedBuffer);
            _engineRemovedBuffer.Clear();
        }
    }

    private void OnEngineAdded(object? sender, Cluster.Model.Engine engine)
    {
        if (sender is null)
            return;

        foreach (var e in _engineAddedBuffer)
        {
            if (e.Engine == engine)
                return;
        }

        _engineAddedBuffer.Add(new((EngineHost)sender, engine));
        ScheduleBufferFlush();
    }

    private void OnEngineHostAdded(object? sender, EngineHost engineHost)
    {
        if (sender is null)
            return;

        foreach (var e in _engineHostAddedBuffer)
        {
            if (e.EngineHost == engineHost)
                return;
        }

        _engineHostAddedBuffer.Add(new((ClusterApplication)sender, engineHost));
        ScheduleBufferFlush();
    }

    private void OnEngineHostRemoved(object? sender, EngineHost engineHost)
    {
        if (sender is null)
            return;

        foreach (var e in _engineHostRemovedBuffer)
        {
            if (e.EngineHost == engineHost)
                return;
        }

        _engineHostRemovedBuffer.Add(new((ClusterApplication)sender, engineHost));
        ScheduleBufferFlush();
    }

    private void OnEngineRemoved(object? sender, Cluster.Model.Engine engine)
    {
        if (sender is null)
            return;

        foreach (var e in _engineRemovedBuffer)
        {
            if (e.Engine == engine)
                return;
        }

        _engineRemovedBuffer.Add(new((EngineHost)sender, engine));
        ScheduleBufferFlush();
    }
}
