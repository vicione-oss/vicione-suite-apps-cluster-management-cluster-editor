using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Builder;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Library.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

internal class DataManagementService(IDatastore datastore, DiagramService diagramService, ILibraryService libraryService, ILogger<DataManagementService> logger) : IDataManagementService
{
    private readonly IDatastore _datastore = datastore;
    private readonly DiagramService _diagramService = diagramService;
    private readonly ILibraryService _libraryService = libraryService;

    public event Func<Task>? ExportRequested;
    public event Func<Task>? ImportRequested;
    public event Func<Task>? LoadFunctionBlockDesignsRequested;
    public event Func<LogLevel, string, Action, Task>? MessageToastRequested;
    public event Func<Task>? NewRequested;
    public event Func<ClusterBuilder, Task>? SaveRequested;

    public Task ForceRootContainerReload()
        => _datastore.LoadContainer(_datastore.Builder.Cluster.Dataflows.First().Root, _diagramService, true);

    public Task LoadDataflow(ClusterBuilder builder)
    {
        builder.InitSettings();
        _libraryService.CreateLibraryEntries(builder.GetFunctionBlockDesigns());

        return _datastore.Load(builder, _diagramService);
    }

    public void LoadFunctionBlockDesigns(IEnumerable<Guid> fbDesigns)
    {
        var builder = _datastore.Builder;

        foreach (var design in fbDesigns)
            builder.Editors.FunctionBlockDesign.AddFunctionBlockDesign(design);

        _libraryService.CreateLibraryEntries(builder.GetFunctionBlockDesigns());
    }

    public void PrepareClusterSerialization()
        => _datastore.SaveViewport(_diagramService);

    public async Task RequestExport()
        => await ExportRequested.InvokeEventAsync(logger, nameof(ExportRequested));

    public async Task RequestImport()
        => await ImportRequested.InvokeEventAsync(logger, nameof(ImportRequested));

    public async Task RequestLoadFbDesigns()
        => await LoadFunctionBlockDesignsRequested.InvokeEventAsync(logger, nameof(LoadFunctionBlockDesignsRequested));

    public void RequestNew()
        => NewRequested?.Invoke();

    public async Task RequestSave()
    {
        _datastore.SaveViewport(_diagramService);
        await SaveRequested.InvokeEventAsync(_datastore.Builder, logger, nameof(SaveRequested));
    }

    public async Task ShowMessageToast(LogLevel logLevel, string message, Action clickCallback)
        => await MessageToastRequested.InvokeEventAsync(logLevel, message, clickCallback, logger, nameof(MessageToastRequested));
}
