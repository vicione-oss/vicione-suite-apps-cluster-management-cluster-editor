using Microsoft.Extensions.Logging;
using Shared.ClusterSerialization;
using Shared.Designs;
using Shared.Extensions;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.ClusterManagement.Services;

/// <summary>
/// Owns the life cycle of the cluster that is currently being edited: it creates the <see cref="IClusterBuilder"/>,
/// keeps the cluster editor in sync with it and disposes it. The boundary towards the persistence is the cluster JSON.
/// </summary>
public sealed partial class ClusterManagementService : IAsyncDisposable
{
    private readonly IClusterEditorManagement _clusterEditorManagement;
    private IDependencyResolver _dependencyResolver = default!;
    private readonly IDesignProvider _designProvider;
    private bool _disposed;
    private CancellationTokenSource _loadClusterCts = new();
    private readonly SemaphoreSlim _loadClusterCtsSemaphore = new(1);
    private readonly ILogger<ClusterManagementService> _logger;

    public IClusterBuilder Builder { get; private set; } = default!;

    public ClusterManagementService(
        IClusterEditorManagement clusterEditorManagement,
        IDesignProvider designProvider,
        ILogger<ClusterManagementService> logger)
    {
        _clusterEditorManagement = clusterEditorManagement;
        _designProvider = designProvider;
        _logger = logger;

        _clusterEditorManagement.LoadFunctionBlockDesignsRequested += OnDataManagementLoadFunctionBlockDesignsRequested;
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

    private async Task LoadCluster(IClusterBuilder builder)
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
    /// Replaces the current cluster with the one described by the given JSON.
    /// </summary>
    /// <param name="clusterJson">The JSON of the cluster to load.</param>
    public Task LoadClusterJson(string clusterJson)
#pragma warning disable CA2000 // Dispose objects before losing scope
        => LoadCluster(CreateBuilder(clusterJson));
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

    private async Task OnDataManagementLoadFunctionBlockDesignsRequested()
        => await LoadFunctionBlockDesignsIntoManagement();

    public void PrepareClusterSerialization()
        => _clusterEditorManagement.PrepareClusterSerialization();
}
