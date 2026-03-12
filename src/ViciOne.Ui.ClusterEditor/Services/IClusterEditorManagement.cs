using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Services;


public interface IClusterEditorManagement
{
    event Func<Task>? ExportRequested;
    event Func<Task>? ImportRequested;
    event Func<Task>? LoadFunctionBlockDesignsRequested;
    event Func<LogLevel, string, Action, Task>? MessageToastRequested;
    event Func<Task>? NewRequested;
    event Func<IClusterBuilder, Task>? SaveRequested;

    Task ForceRootContainerReload();
    Task LoadDataflow(IClusterBuilder builder);
    void LoadFunctionBlockDesigns(IEnumerable<Guid> fbDesigns);
    void PrepareClusterSerialization();
}
