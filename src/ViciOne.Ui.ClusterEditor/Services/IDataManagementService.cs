using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder;

namespace ViciOne.Ui.ClusterEditor.Services;

public interface IDataManagementService
{
    event Func<Task>? ExportRequested;
    event Func<Task>? ImportRequested;
    event Func<Task>? LoadFunctionBlockDesignsRequested;
    event Func<LogLevel, string, Action, Task>? MessageToastRequested;
    event Func<Task>? NewRequested;
    event Func<ClusterBuilder, Task>? SaveRequested;

    Task ForceRootContainerReload();
    Task LoadDataflow(ClusterBuilder builder);
    void LoadFunctionBlockDesigns(IEnumerable<Guid> fbDesigns);
    void PrepareClusterSerialization();
    Task ShowMessageToast(LogLevel logLevel, string message, Action clickCallback);
}
