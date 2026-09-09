using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Services;


public interface IClusterEditorManagement
{
    event Func<Task>? LoadFunctionBlockDesignsRequested;
    event Func<LogLevel, string, Action, Task>? MessageToastRequested;
    event Func<IClusterBuilder, Task>? SaveRequested;

    Task ForceRootContainerReload(CancellationToken cancellationToken);
    Task LoadDataflow(IClusterBuilder builder, CancellationToken cancellationToken);
    void LoadFunctionBlockDesigns(IEnumerable<Guid> fbDesigns);
    void PrepareClusterSerialization();
}
