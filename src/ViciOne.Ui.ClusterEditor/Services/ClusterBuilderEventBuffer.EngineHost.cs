using System;
using System.Collections.Generic;
using System.Linq;
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
            EnginesAdded?.Invoke([.. _engineAddedBuffer]);
            _engineAddedBuffer.Clear();
        }

        if (_engineHostAddedBuffer.Count > 0)
        {
            EngineHostsAdded?.Invoke([.. _engineHostAddedBuffer]);
            _engineHostAddedBuffer.Clear();
        }

        if (_engineHostRemovedBuffer.Count > 0)
        {
            EngineHostsRemoved?.Invoke([.. _engineHostRemovedBuffer]);
            _engineHostRemovedBuffer.Clear();
        }

        if (_engineRemovedBuffer.Count > 0)
        {
            EnginesRemoved?.Invoke([.. _engineRemovedBuffer]);
            _engineRemovedBuffer.Clear();
        }
    }

    private void OnEngineAdded(object? sender, Cluster.Model.Engine engine)
    {
        if (_engineAddedBuffer.Any(e => e.Engine == engine) || sender is null)
            return;

        _engineAddedBuffer.Add(new((EngineHost)sender, engine));
        StartBufferTimer();
    }

    private void OnEngineHostAdded(object? sender, EngineHost engineHost)
    {
        if (_engineHostAddedBuffer.Any(e => e.EngineHost == engineHost) || sender is null)
            return;

        _engineHostAddedBuffer.Add(new((ClusterApplication)sender, engineHost));
        StartBufferTimer();
    }

    private void OnEngineHostRemoved(object? sender, EngineHost engineHost)
    {
        if (_engineHostRemovedBuffer.Any(e => e.EngineHost == engineHost) || sender is null)
            return;

        _engineHostRemovedBuffer.Add(new((ClusterApplication)sender, engineHost));
        StartBufferTimer();
    }

    private void OnEngineRemoved(object? sender, Cluster.Model.Engine engine)
    {
        if (_engineRemovedBuffer.Any(e => e.Engine == engine) || sender is null)
            return;

        _engineRemovedBuffer.Add(new((EngineHost)sender, engine));
        StartBufferTimer();
    }
}
