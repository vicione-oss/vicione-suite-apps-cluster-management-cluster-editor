using System.Drawing;
using Microsoft.Extensions.Logging;
using Shared.ClusterSerialization;
using Shared.Designs;
using Shared.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Services;

public sealed partial class IndexService : IAsyncDisposable
{
    private readonly IClusterEditorManagement _clusterEditorManagement;
    private IDependencyResolver _dependencyResolver = default!;
    private readonly IDesignProvider _designProvider;
    private bool _disposed;
    private CancellationTokenSource _forceContainerReloadCts = new();
    private readonly SemaphoreSlim _forceContainerReloadCtsSemaphore = new(1);
    private CancellationTokenSource _loadClusterCts = new();
    private readonly SemaphoreSlim _loadClusterCtsSemaphore = new(1);
    private readonly ILogger<IndexService> _logger;

    public IClusterBuilder Builder { get; private set; } = default!;

    public event Func<IClusterBuilder, Task>? ClusterLoaded;

    public IndexService(
        IClusterEditorManagement clusterEditorManagement,
        IDesignProvider designProvider,
        ILogger<IndexService> logger)
    {
        _clusterEditorManagement = clusterEditorManagement;
        _designProvider = designProvider;
        _logger = logger;

        _clusterEditorManagement.LoadFunctionBlockDesignsRequested += OnDataManagementLoadFunctionBlockDesignsRequested;
    }

    internal async Task AddContainersAndRefresh()
    {
        if (_disposed)
            return;

        var root = Builder.Cluster.Dataflows[0].Root;
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

    private ClusterBuilder? CreateBuilderOrDefault(string? clusterJson)
    {
        if (string.IsNullOrEmpty(clusterJson))
            return null;

        try
        {
            return CreateBuilder(clusterJson);
        }
        catch (Exception ex)
        {
            // Can happen if the cluster model has changed since last save
            LogCreateClusterFailed(_logger, ex);
            return null;
        }
    }

    private IClusterBuilder CreateNewCluster()
        => new ClusterBuilder(_dependencyResolver).AddDemoElements();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        _clusterEditorManagement.LoadFunctionBlockDesignsRequested -= OnDataManagementLoadFunctionBlockDesignsRequested;

        Builder?.Dispose();

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

    /// <summary>
    /// Initializes the cluster from the given JSON, or creates a new one if there is none or it cannot be read.
    /// </summary>
    /// <param name="clusterJson">The JSON of a previously stored cluster, or <see langword="null" />.</param>
    public async Task InitCluster(string? clusterJson)
    {
        _dependencyResolver = _designProvider.CreateResolver();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var newBuilder = CreateBuilderOrDefault(clusterJson) ?? CreateNewCluster();
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

    /// <summary>
    /// Replaces the current cluster with a newly created one.
    /// </summary>
    internal async Task LoadNewCluster()
    {
        Builder = CreateNewCluster();
        await LoadCluster(Builder);

        await LoadFunctionBlockDesignsIntoManagement();

        LogClusterCreation(_logger, Builder.Cluster.Id);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created a new ClusterBuilder with Id {ClusterId}.")]
    private static partial void LogClusterCreation(ILogger logger, Guid clusterId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create a cluster from the stored JSON.")]
    private static partial void LogCreateClusterFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {FnName} event handler.")]
    private static partial void LogEventHandlerException(ILogger logger, Exception ex, string fnName);


    private async Task OnDataManagementLoadFunctionBlockDesignsRequested()
        => await LoadFunctionBlockDesignsIntoManagement();

    public void PrepareClusterSerialization()
        => _clusterEditorManagement.PrepareClusterSerialization();
}
