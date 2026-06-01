using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer
{
    private readonly List<FunctionBlock> _engineAssignedBuffer = [];
    private readonly List<FunctionBlock> _engineUnassignedBuffer = [];
    private readonly List<(Container Parent, FunctionBlock FunctionBlock)> _functionBlockAddedBuffer = [];
    private readonly List<(Cluster.Model.Cluster Parent, Guid DesignId)> _functionBlockDesignAddedBuffer = [];
    private readonly List<(Cluster.Model.Cluster Parent, Guid DesignId)> _functionBlockDesignRemovedBuffer = [];
    private readonly List<(object? s, System.ComponentModel.PropertyChangedEventArgs e)> _functionBlockPropertyChangedBuffer = [];
    private readonly List<(Container Parent, FunctionBlock FunctionBlock)> _functionBlockRemovedBuffer = [];

    public event Action<IEnumerable<FunctionBlock>>? EnginesAssigned;
    public event Action<IEnumerable<FunctionBlock>>? EnginesUnassigned;
    public event Action<IEnumerable<(Cluster.Model.Cluster Parent, Guid DesignId)>>? FunctionBlockDesignsAdded;
    public event Action<IEnumerable<(Cluster.Model.Cluster Parent, Guid DesignId)>>? FunctionBlockDesignsRemoved;
    public event Action<IEnumerable<(object? Sender, System.ComponentModel.PropertyChangedEventArgs EventArgs)>>? FunctionBlockPropertiesChanged;
    public event Action<IEnumerable<(Container Parent, FunctionBlock FunctionBlock)>>? FunctionBlocksAdded;
    public event Action<IEnumerable<(Container Parent, FunctionBlock FunctionBlock)>>? FunctionBlocksRemoved;

    private void AttachFunctionBlockEvents()
    {
        Builder.Editors.Container.FunctionBlockAdded += OnFunctionBlockAdded;
        Builder.Editors.Container.FunctionBlockRemoved += OnFunctionBlockRemoved;
        Builder.Editors.FunctionBlock.EngineAssigned += OnEngineAssigned;
        Builder.Editors.FunctionBlock.EngineUnassigned += OnEngineUnassigned;
        Builder.Editors.FunctionBlock.PropertyChanged += OnFunctionBlockPropertyChanged;
        Builder.Editors.FunctionBlockDesign.FunctionBlockDesignAdded += OnFunctionBlockDesignAdded;
        Builder.Editors.FunctionBlockDesign.FunctionBlockDesignRemoved += OnFunctionBlockDesignRemoved;
    }

    private void DetachFunctionBlockEvents()
    {
        Builder.Editors.Container.FunctionBlockAdded -= OnFunctionBlockAdded;
        Builder.Editors.Container.FunctionBlockRemoved -= OnFunctionBlockRemoved;
        Builder.Editors.FunctionBlock.EngineAssigned -= OnEngineAssigned;
        Builder.Editors.FunctionBlock.EngineUnassigned -= OnEngineUnassigned;
        Builder.Editors.FunctionBlock.PropertyChanged -= OnFunctionBlockPropertyChanged;
        Builder.Editors.FunctionBlockDesign.FunctionBlockDesignAdded -= OnFunctionBlockDesignAdded;
        Builder.Editors.FunctionBlockDesign.FunctionBlockDesignRemoved -= OnFunctionBlockDesignRemoved;
    }

    private void FireFunctionBlockEvents()
    {
        if (_engineAssignedBuffer.Count > 0)
        {
            EnginesAssigned?.Invoke([.. _engineAssignedBuffer]);
            _engineAssignedBuffer.Clear();
        }

        if (_engineUnassignedBuffer.Count > 0)
        {
            EnginesUnassigned?.Invoke([.. _engineUnassignedBuffer]);
            _engineUnassignedBuffer.Clear();
        }

        if (_functionBlockAddedBuffer.Count > 0)
        {
            FunctionBlocksAdded?.Invoke([.. _functionBlockAddedBuffer]);
            _functionBlockAddedBuffer.Clear();
        }

        if (_functionBlockDesignAddedBuffer.Count > 0)
        {
            FunctionBlockDesignsAdded?.Invoke([.. _functionBlockDesignAddedBuffer]);
            _functionBlockDesignAddedBuffer.Clear();
        }

        if (_functionBlockDesignRemovedBuffer.Count > 0)
        {
            FunctionBlockDesignsRemoved?.Invoke([.. _functionBlockDesignRemovedBuffer]);
            _functionBlockDesignRemovedBuffer.Clear();
        }

        if (_functionBlockPropertyChangedBuffer.Count > 0)
        {
            FunctionBlockPropertiesChanged?.Invoke([.. _functionBlockPropertyChangedBuffer]);
            _functionBlockPropertyChangedBuffer.Clear();
        }

        if (_functionBlockRemovedBuffer.Count > 0)
        {
            FunctionBlocksRemoved?.Invoke([.. _functionBlockRemovedBuffer]);
            _functionBlockRemovedBuffer.Clear();
        }
    }

    private void OnEngineAssigned(IEnumerable<FunctionBlock> fbs)
    {
        foreach (var fb in fbs)
        {
            if (_engineAssignedBuffer.Contains(fb))
                continue;

            _engineAssignedBuffer.Add(fb);
            ScheduleBufferFlush();
        }
    }

    private void OnEngineUnassigned(IEnumerable<FunctionBlock> fbs)
    {
        foreach (var fb in fbs)
        {
            if (_engineUnassignedBuffer.Contains(fb))
                continue;

            _engineUnassignedBuffer.Add(fb);
            ScheduleBufferFlush();
        }
    }

    private void OnFunctionBlockAdded(object? sender, FunctionBlock functionBlock)
    {
        if (_functionBlockAddedBuffer.Any(fb => fb.FunctionBlock == functionBlock) || sender is null)
            return;

        _functionBlockAddedBuffer.Add(new((Container)sender, functionBlock));
        ScheduleBufferFlush();
    }

    private void OnFunctionBlockDesignAdded(object? sender, Guid designId)
    {
        if (_functionBlockDesignAddedBuffer.Any(id => id.DesignId == designId) || sender is null)
            return;

        _functionBlockDesignAddedBuffer.Add(new((Cluster.Model.Cluster)sender, designId));
        ScheduleBufferFlush();
    }

    private void OnFunctionBlockDesignRemoved(object? sender, Guid designId)
    {
        if (_functionBlockDesignRemovedBuffer.Any(id => id.DesignId == designId) || sender is null)
            return;

        _functionBlockDesignRemovedBuffer.Add(new((Cluster.Model.Cluster)sender, designId));
        ScheduleBufferFlush();
    }

    public void OnFunctionBlockPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _functionBlockPropertyChangedBuffer.Add((s, e));
        ScheduleBufferFlush();
    }

    private void OnFunctionBlockRemoved(object? sender, FunctionBlock functionBlock)
    {
        if (_functionBlockRemovedBuffer.Any(fb => fb.FunctionBlock == functionBlock) || sender is null)
            return;

        _functionBlockRemovedBuffer.Add(new((Container)sender, functionBlock));
        ScheduleBufferFlush();
    }
}
