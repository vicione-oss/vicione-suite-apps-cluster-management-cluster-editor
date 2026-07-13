using System;
using System.Collections.Generic;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.Data;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Holds the mutable cluster editor state for a circuit and raises change
/// notifications. Behavior services mutate this state and raise events through
/// it; read-only collaborators consume it via <see cref="IDatastoreState"/>.
/// </summary>
internal sealed class DatastoreState : IDatastoreState
{
    private IClusterBuilder? _builder;

    public Container ActiveContainer { get; private set; } = new();
    public Dataflow ActiveDataflow { get; private set; } = new();
    public IClusterBuilder Builder => _builder ?? throw new InvalidOperationException($"Use method {nameof(IDatastore.Load)} to init the builder");
    public DataflowDiagramMapping DataflowDiagramMapping { get; } = new();
    public bool HasBuilder => _builder is not null;
    public IEnumerable<Cluster.Model.Engine> ValidDataflowEngines { get; private set; } = [];

    public event Action? ActiveDataflowChanged;
    public event Action? BuilderChanged;
    public event Action<BlockNodeLink>? ConnectorLinkRemoved;
    public event Action<ChildContainer, string>? ContainerPropertyChanged;
    public event Action? ForcedRefreshRequested;
    public event Action<string>? PropertyChanged;

    public void InvokeBuilderChanged()
        => BuilderChanged?.Invoke();

    public void InvokeConnectorLinkRemoved(BlockNodeLink nodeLink)
        => ConnectorLinkRemoved?.Invoke(nodeLink);

    public void InvokeContainerPropertyChanged(ChildContainer container, string propertyName)
        => ContainerPropertyChanged?.Invoke(container, propertyName);

    public void InvokeForcedRefreshRequested()
        => ForcedRefreshRequested?.Invoke();

    public void InvokePropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(propertyName);

    public void SetActiveContainer(Container container)
        => ActiveContainer = container;

    public void SetActiveDataflow(Dataflow dataflow)
    {
        if (dataflow == ActiveDataflow)
            return;

        ActiveDataflow = dataflow;
        ActiveDataflowChanged?.Invoke();
    }

    public void SetBuilder(IClusterBuilder builder)
        => _builder = builder;

    public void SetValidDataflowEngines(IEnumerable<Cluster.Model.Engine> engines)
        => ValidDataflowEngines = engines;
}
