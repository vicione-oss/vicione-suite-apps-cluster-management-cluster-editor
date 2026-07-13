using System.Collections.Generic;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.Data;

namespace ViciOne.Ui.ClusterEditor.Services.ClusterServices;

/// <summary>
/// Read-only view over the cluster editor state shared within a circuit.
/// Collaborators that only need to project or inspect the active container
/// (mappers, diagram models, the dragging-link event) depend on this narrow
/// surface instead of the full <see cref="IDatastore"/> to keep coupling low.
/// </summary>
public interface IDatastoreState
{
    Container ActiveContainer { get; }
    Dataflow ActiveDataflow { get; }
    IClusterBuilder Builder { get; }
    DataflowDiagramMapping DataflowDiagramMapping { get; }
    bool HasBuilder { get; }
    IEnumerable<Cluster.Model.Engine> ValidDataflowEngines { get; }
}
