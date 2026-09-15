using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Services;


public interface IClusterEditorManagement
{
    /// <summary>
    /// The container the editor is currently showing. Hosts that create cluster elements through the
    /// <see cref="IClusterBuilder"/> use it as the parent for their creations, so that the elements end
    /// up where the user is looking.
    /// </summary>
    Container ActiveContainer { get; }

    event Func<Task>? LoadFunctionBlockDesignsRequested;
    event Func<LogLevel, string, Action, Task>? MessageToastRequested;
    event Func<IClusterBuilder, Task>? SaveRequested;

    Task ForceRootContainerReload(CancellationToken cancellationToken);
    Task LoadDataflow(IClusterBuilder builder, CancellationToken cancellationToken);
    void LoadFunctionBlockDesigns(IEnumerable<Guid> fbDesigns);
    void PrepareClusterSerialization();

    /// <summary>
    /// Rebuilds the diagram of the <see cref="ActiveContainer"/> from the cluster model. Hosts call this
    /// after they mutated the cluster through the <see cref="IClusterBuilder"/>, because the editor does
    /// not project externally added function blocks, containers, labels or links on its own.
    /// </summary>
    Task ReloadActiveContainer(CancellationToken cancellationToken);
}
