using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.ClusterSerialization;
using Shared.Designs;
using Shared.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Services;

public sealed class IndexService : IDisposable
{
    private const int DefaultSaveSlot = 4;

    private CancellationTokenSource? _createClusterFromJsCts;
    private readonly IDataManagementService _dataManagementService;
    private IDependencyResolver _dependencyResolver = default!;
    private readonly IDesignProvider _designProvider;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<IndexService> _logger;

    public ClusterBuilder Builder { get; private set; } = default!;
    public IDesignProvider DesignLoader => _designProvider;
    public bool DisplayDebugArea { get; private set; }
    public Action? StateHasChanged { get; set; }

    public event Func<ClusterBuilder, Task>? ClusterLoaded;
    public event Func<Task>? SaveFailed;

    public IndexService(
        IDataManagementService dataManagementService,
        IDesignProvider designProvider,
        IJSRuntime jsRuntime,
        ILogger<IndexService> logger)
    {
        _dataManagementService = dataManagementService;
        _designProvider = designProvider;
        _jsRuntime = jsRuntime;
        _logger = logger;

        _dataManagementService.ExportRequested += OnDataManagementExportRequested;
        _dataManagementService.ImportRequested += OnDataManagementImportRequested;
        _dataManagementService.LoadFunctionBlockDesignsRequested += OnDataManagementLoadFunctionBlockDesignsRequested;
        _dataManagementService.NewRequested += OnDataManagementNewRequested;
        _dataManagementService.SaveRequested += OnDataManagementServiceSaveRequested;
    }

    internal async Task AddContainersAndRefresh()
    {
        var root = Builder.Cluster.Dataflows.First().Root;
        for (var i = 0; i < 5; i++)
            Builder.Editors.Container.AddContainer(root, $"Generated {i}", location: new Point(i * 200, 0));

        await _dataManagementService.ForceRootContainerReload();
    }

    private ClusterBuilder CreateBuilder(string clusterJson)
    {
        var cluster = ClusterSerializer.Deserialize(clusterJson);
        return new ClusterBuilder(cluster, _dependencyResolver);
    }

    private async Task<ClusterBuilder?> CreateClusterFromJs()
    {
        if (_createClusterFromJsCts is not null)
        {
            await _createClusterFromJsCts.CancelAsync();
            _createClusterFromJsCts.Dispose();
        }

        _createClusterFromJsCts = new CancellationTokenSource();

        try
        {
            if (await _jsRuntime.InvokeAsync<bool>("ViciOne.File.hasValue", _createClusterFromJsCts.Token, DefaultSaveSlot))
            {
                var json = await _jsRuntime.InvokeAsync<string>("ViciOne.File.load", _createClusterFromJsCts.Token, DefaultSaveSlot);
                return CreateBuilder(json);
            }
        }
        catch (TaskCanceledException ex)
        {
            // Expected during fast reload - suppress the exception
            _logger.LogDebug(ex, $"{nameof(TaskCanceledException)} during {nameof(CreateClusterFromJs)}");
        }
        catch (OperationCanceledException ex)
        {
            // Expected if the operation was canceld - suppress the exception
            _logger.LogDebug(ex, $"{nameof(OperationCanceledException)} during {nameof(CreateClusterFromJs)}");
        }
        catch (Exception ex)
        {
            // can happen if the cluster model has changed since last save
            _logger.LogError(ex, "Failed to load cluster via JS.");
        }

        return null;
    }

    private ClusterBuilder CreateNewCluster()
        => new ClusterBuilder(_dependencyResolver).AddDemoElements();

    public void Dispose()
    {
        _dataManagementService.ExportRequested -= OnDataManagementExportRequested;
        _dataManagementService.ImportRequested -= OnDataManagementImportRequested;
        _dataManagementService.LoadFunctionBlockDesignsRequested -= OnDataManagementLoadFunctionBlockDesignsRequested;
        _dataManagementService.NewRequested -= OnDataManagementNewRequested;
        _dataManagementService.SaveRequested -= OnDataManagementServiceSaveRequested;

        Builder?.Dispose();

        _createClusterFromJsCts?.Cancel();
        _createClusterFromJsCts?.Dispose();
    }

    public async Task InitCluster()
    {
        _dependencyResolver = _designProvider.CreateResolver();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var newBuilder = await CreateClusterFromJs() ?? CreateNewCluster();
#pragma warning restore CA2000 // Dispose objects before losing scope

        LoadCluster(newBuilder);

        await LoadFunctionBlockDesignsIntoManagement();
    }

    private async Task InvokeClusterLoaded(ClusterBuilder builder)
    {
        if (ClusterLoaded is null)
            return;

        var tasks = ClusterLoaded.GetInvocationList()
            .Cast<Func<ClusterBuilder, Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler(builder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception in {nameof(IndexService)}.{nameof(ClusterLoaded)} event handler");
                }
            });

        await Task.WhenAll(tasks);
    }

    public async Task InvokeSaveFailed()
    {
        if (SaveFailed is null)
            return;

        var tasks = SaveFailed.GetInvocationList()
            .Cast<Func<Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception in {nameof(IndexService)}.{nameof(SaveFailed)} event handler");
                }
            });

        await Task.WhenAll(tasks);
    }

    public void LoadCluster(ClusterBuilder builder)
    {
        Builder = builder;

        _dataManagementService.LoadDataflow(Builder);
    }

    /// <summary>
    /// Simulate async cluster loading similar to the Suite
    /// </summary>
    /// <param name="clusterJson"></param>
    /// <returns></returns>
    public Task LoadClusterJson(string clusterJson)
#pragma warning disable CA2000 // Dispose objects before losing scope
        => InvokeClusterLoaded(CreateBuilder(clusterJson));
#pragma warning restore CA2000 // Dispose objects before losing scope

    private async Task LoadFunctionBlockDesignsIntoManagement()
    {
        var fbDesigns = await _designProvider.GetFunctionBlockDesignIds(null);
        _dataManagementService.LoadFunctionBlockDesigns(fbDesigns);
    }

    public void OnCloseRequested()
    {
        DisplayDebugArea = false;
        StateHasChanged?.Invoke();
    }

    private async Task OnDataManagementExportRequested()
    {
        var json = ClusterSerializer.Serialize(Builder.Cluster);
        await _jsRuntime.InvokeVoidAsync("ViciOne.File.download", json);
    }

    private Task OnDataManagementImportRequested()
    {
        DisplayDebugArea = true;
        StateHasChanged?.Invoke();
        return Task.CompletedTask;
    }

    private async Task OnDataManagementLoadFunctionBlockDesignsRequested()
        => await LoadFunctionBlockDesignsIntoManagement();

    private async Task OnDataManagementNewRequested()
    {
        Builder = CreateNewCluster();
        LoadCluster(Builder);

        await LoadFunctionBlockDesignsIntoManagement();

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Created a new ClusterBuilder with ID {ClusterId}", Builder.Cluster.Id);
    }

    private async Task OnDataManagementServiceSaveRequested(ClusterBuilder builder)
    {
        var json = ClusterSerializer.Serialize(Builder.Cluster);
        var success = await _jsRuntime.InvokeAsync<bool>("ViciOne.File.save", DefaultSaveSlot, json);

        if (!success)
            await InvokeSaveFailed();
    }

    public void PrepareClusterSerialization()
        => _dataManagementService.PrepareClusterSerialization();
}
