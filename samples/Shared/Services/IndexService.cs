using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.ClusterSerialization;
using Shared.Designs;
using Shared.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Services;

public sealed partial class IndexService : IAsyncDisposable
{
    private const int DefaultSaveSlot = 4;

    private readonly IClusterEditorManagement _clusterEditorManagement;
    private CancellationTokenSource _createClusterFromJsCts = new();
    private readonly SemaphoreSlim _createClusterFromJsCtsSemaphore = new(1);
    private IDependencyResolver _dependencyResolver = default!;
    private readonly IDesignProvider _designProvider;
    private bool _disposed;
    private CancellationTokenSource _forceContainerReloadCts = new();
    private readonly SemaphoreSlim _forceContainerReloadCtsSemaphore = new(1);
    private readonly IJSRuntime _jsRuntime;
    private CancellationTokenSource _loadClusterCts = new();
    private readonly SemaphoreSlim _loadClusterCtsSemaphore = new(1);
    private readonly ILogger<IndexService> _logger;

    public IClusterBuilder Builder { get; private set; } = default!;
    public IDesignProvider DesignLoader => _designProvider;
    public bool DisplayDebugArea { get; private set; }
    public Action? StateHasChanged { get; set; }

    public event Func<IClusterBuilder, Task>? ClusterLoaded;
    public event Func<Task>? SaveFailed;

    public IndexService(
        IClusterEditorManagement clusterEditorManagement,
        IDesignProvider designProvider,
        IJSRuntime jsRuntime,
        ILogger<IndexService> logger)
    {
        _clusterEditorManagement = clusterEditorManagement;
        _designProvider = designProvider;
        _jsRuntime = jsRuntime;
        _logger = logger;

        _clusterEditorManagement.ExportRequested += OnDataManagementExportRequested;
        _clusterEditorManagement.ImportRequested += OnDataManagementImportRequested;
        _clusterEditorManagement.LoadFunctionBlockDesignsRequested += OnDataManagementLoadFunctionBlockDesignsRequested;
        _clusterEditorManagement.NewRequested += OnDataManagementNewRequested;
        _clusterEditorManagement.SaveRequested += OnDataManagementServiceSaveRequested;
    }

    internal async Task AddContainersAndRefresh()
    {
        if (_disposed)
            return;

        var root = Builder.Cluster.Dataflows.First().Root;
        for (var i = 0; i < 5; i++)
            Builder.Editors.Container.AddContainer(root, $"Generated {i}", location: new Point(i * 200, 0));

        try
        {
            await _forceContainerReloadCtsSemaphore.WaitAsync();
            try
            {
                await _forceContainerReloadCts.CancelAsync();
                _forceContainerReloadCts.Dispose();
                _forceContainerReloadCts = new CancellationTokenSource();

                await _clusterEditorManagement.ForceRootContainerReload(_forceContainerReloadCts.Token);
            }
            finally
            {
                _forceContainerReloadCtsSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Nothing to do here, return gracefully
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // Semaphore or other object already disposed, nothing we can do, return gracefully
        }
    }

    private ClusterBuilder CreateBuilder(string clusterJson)
    {
        var cluster = ClusterSerializer.Deserialize(clusterJson);
        return new ClusterBuilder(cluster, _dependencyResolver);
    }

    private async Task<IClusterBuilder?> CreateClusterFromJs()
    {
        if (_disposed)
            return null;

        try
        {
            await _createClusterFromJsCtsSemaphore.WaitAsync();
            try
            {
                await _createClusterFromJsCts.CancelAsync();
                _createClusterFromJsCts.Dispose();
                _createClusterFromJsCts = new CancellationTokenSource();

                if (await _jsRuntime.InvokeAsync<bool>("ViciOne.File.hasValue", _createClusterFromJsCts.Token, DefaultSaveSlot))
                {
                    var json = await _jsRuntime.InvokeAsync<string>("ViciOne.File.load", _createClusterFromJsCts.Token, DefaultSaveSlot);
                    return CreateBuilder(json);
                }
            }
            finally
            {
                _createClusterFromJsCtsSemaphore.Release();
            }
        }
        catch (TaskCanceledException ex)
        {
            // Expected during fast reload - suppress the exception
            LogCreateClusterCanceled(_logger, ex);
        }
        catch (OperationCanceledException ex)
        {
            // Expected if the operation was canceled - suppress the exception
            LogCreateClusterCanceled(_logger, ex);
        }
        catch (Exception ex)
        {
            // Can happen if the cluster model has changed since last save
            LogCreateClusterFailed(_logger, ex);
        }

        return null;
    }

    private IClusterBuilder CreateNewCluster()
        => new ClusterBuilder(_dependencyResolver).AddDemoElements();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        _clusterEditorManagement.ExportRequested -= OnDataManagementExportRequested;
        _clusterEditorManagement.ImportRequested -= OnDataManagementImportRequested;
        _clusterEditorManagement.LoadFunctionBlockDesignsRequested -= OnDataManagementLoadFunctionBlockDesignsRequested;
        _clusterEditorManagement.NewRequested -= OnDataManagementNewRequested;
        _clusterEditorManagement.SaveRequested -= OnDataManagementServiceSaveRequested;

        Builder?.Dispose();

        await _createClusterFromJsCtsSemaphore.WaitAsync();
        try
        {
            await _createClusterFromJsCts.CancelAsync();
            _createClusterFromJsCts.Dispose();
        }
        finally
        {
            _createClusterFromJsCtsSemaphore.Release();
        }

        _createClusterFromJsCtsSemaphore.Dispose();

        await _loadClusterCtsSemaphore.WaitAsync();
        try
        {
            await _loadClusterCts.CancelAsync();
            _loadClusterCts.Dispose();
        }
        finally
        {
            _loadClusterCtsSemaphore.Release();
        }

        _loadClusterCtsSemaphore.Dispose();

        await _forceContainerReloadCtsSemaphore.WaitAsync();
        try
        {
            await _forceContainerReloadCts.CancelAsync();
            _forceContainerReloadCts.Dispose();
        }
        finally
        {
            _forceContainerReloadCtsSemaphore.Release();
        }

        _forceContainerReloadCtsSemaphore.Dispose();
    }

    public async Task InitCluster()
    {
        _dependencyResolver = _designProvider.CreateResolver();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var newBuilder = await CreateClusterFromJs() ?? CreateNewCluster();
#pragma warning restore CA2000 // Dispose objects before losing scope

        await LoadCluster(newBuilder);

        await LoadFunctionBlockDesignsIntoManagement();
    }

    private async Task InvokeClusterLoaded(IClusterBuilder builder)
    {
        if (ClusterLoaded is null)
            return;

        var tasks = ClusterLoaded.GetInvocationList()
            .Cast<Func<IClusterBuilder, Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler(builder);
                }
                catch (Exception ex)
                {
                    LogEventHandlerException(_logger, ex, $"{nameof(IndexService)}.{nameof(ClusterLoaded)}");
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
                    LogEventHandlerException(_logger, ex, $"{nameof(IndexService)}.{nameof(SaveFailed)}");
                }
            });

        await Task.WhenAll(tasks);
    }

    public async Task LoadCluster(IClusterBuilder builder)
    {
        if (_disposed)
            return;

        Builder = builder;

        try
        {
            await _loadClusterCtsSemaphore.WaitAsync();
            try
            {
                await _loadClusterCts.CancelAsync();
                _loadClusterCts.Dispose();
                _loadClusterCts = new CancellationTokenSource();

                await _clusterEditorManagement.LoadDataflow(Builder, _loadClusterCts.Token);
            }
            finally
            {
                _loadClusterCtsSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Nothing to do here, return gracefully
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // Semaphore or other object already disposed, nothing we can do, return gracefully
        }
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
        _clusterEditorManagement.LoadFunctionBlockDesigns(fbDesigns);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created a new ClusterBuilder with Id {ClusterId}.")]
    private static partial void LogClusterCreation(ILogger logger, Guid clusterId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "CreateClusterFromJs canceled.")]
    private static partial void LogCreateClusterCanceled(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load cluster via JavaScript.")]
    private static partial void LogCreateClusterFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {FnName} event handler.")]
    private static partial void LogEventHandlerException(ILogger logger, Exception ex, string fnName);

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
        await LoadCluster(Builder);

        await LoadFunctionBlockDesignsIntoManagement();

        LogClusterCreation(_logger, Builder.Cluster.Id);
    }

    private async Task OnDataManagementServiceSaveRequested(IClusterBuilder builder)
    {
        var json = ClusterSerializer.Serialize(Builder.Cluster);
        var success = await _jsRuntime.InvokeAsync<bool>("ViciOne.File.save", DefaultSaveSlot, json);

        if (!success)
            await InvokeSaveFailed();
    }

    public void PrepareClusterSerialization()
        => _clusterEditorManagement.PrepareClusterSerialization();
}
