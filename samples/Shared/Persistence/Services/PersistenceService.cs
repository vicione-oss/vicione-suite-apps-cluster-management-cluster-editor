using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.ClusterManagement.Services;
using Shared.ClusterSerialization;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Persistence.Services;

/// <summary>
/// Owns everything that is needed to persist a cluster: the browser storage, the file download and upload
/// and the serialization. The boundary towards <see cref="ClusterManagementService"/> is the cluster JSON.
/// </summary>
public sealed partial class PersistenceService : IAsyncDisposable
{
    private const int MaxUploadFileSize = 1024 * 1024 * 1024;
    internal const int SaveSlotCount = 3;
    internal const int StartUpSaveSlot = SaveSlotCount + 1;

    private static readonly int[] s_allSaveSlots = [.. Enumerable.Range(1, StartUpSaveSlot)];

    private readonly IClusterEditorManagement _clusterEditorManagement;
    private readonly ClusterManagementService _clusterManagementService;
    private bool _disposed;
    private readonly IJSRuntime _jsRuntime;
    private CancellationTokenSource _loadStoredClusterJsonCts = new();
    private readonly SemaphoreSlim _loadStoredClusterJsonCtsSemaphore = new(1);
    private readonly ILogger<PersistenceService> _logger;

    public event Func<Task>? SaveFailed;

    public PersistenceService(
        IClusterEditorManagement clusterEditorManagement,
        ClusterManagementService clusterManagementService,
        IJSRuntime jsRuntime,
        ILogger<PersistenceService> logger)
    {
        _clusterEditorManagement = clusterEditorManagement;
        _clusterManagementService = clusterManagementService;
        _jsRuntime = jsRuntime;
        _logger = logger;

        _clusterEditorManagement.SaveRequested += OnSaveRequested;
    }

    internal async Task ClearSaveSlot(int saveSlot)
        => await TryInvokeVoid("ViciOne.File.clear", saveSlot);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        _clusterEditorManagement.SaveRequested -= OnSaveRequested;

        await _loadStoredClusterJsonCtsSemaphore.WaitAsync();
        try
        {
            await _loadStoredClusterJsonCts.CancelAsync();
            _loadStoredClusterJsonCts.Dispose();
        }
        finally
        {
            _loadStoredClusterJsonCtsSemaphore.Release();
        }

        _loadStoredClusterJsonCtsSemaphore.Dispose();
    }

    internal async Task ExportCluster()
    {
        var json = SerializeCurrentCluster();

        if (json is null)
            return;

        await TryInvokeVoid("ViciOne.File.download", json);
    }

    /// <summary>
    /// Reads the size in kilobytes of every save slot, or <see langword="null" /> if the sizes cannot be read.
    /// An empty save slot reports a size of <c>-1</c>.
    /// </summary>
    internal async Task<IReadOnlyDictionary<int, int>?> GetSaveSlotSizes()
    {
        var (succeeded, saveSlotSizes) = await TryInvoke<int[]>("ViciOne.File.getSizes", CancellationToken.None, s_allSaveSlots);

        if (!succeeded || saveSlotSizes is null || saveSlotSizes.Length != s_allSaveSlots.Length)
            return null;

        return s_allSaveSlots
            .Select((saveSlot, index) => (SaveSlot: saveSlot, Size: saveSlotSizes[index]))
            .ToDictionary(entry => entry.SaveSlot, entry => entry.Size);
    }

    internal async Task ImportCluster(IBrowserFile file)
    {
        try
        {
            await using MemoryStream ms = new();
            await file.OpenReadStream(MaxUploadFileSize).CopyToAsync(ms);
            var json = Encoding.UTF8.GetString(ms.ToArray());

            await _clusterManagementService.LoadClusterJson(json);
        }
        catch (Exception ex)
        {
            LogFailedImport(_logger, ex, file.Name);
        }
    }

    private async Task InvokeSaveFailed()
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
                    LogEventHandlerException(_logger, ex, $"{nameof(PersistenceService)}.{nameof(SaveFailed)}");
                }
            });

        await Task.WhenAll(tasks);
    }

    internal async Task LoadFromSaveSlot(int saveSlot)
    {
        var (succeeded, json) = await TryInvoke<string>("ViciOne.File.load", CancellationToken.None, saveSlot);

        if (!succeeded || string.IsNullOrEmpty(json))
            return;

        try
        {
            await _clusterManagementService.LoadClusterJson(json);
        }
        catch (Exception ex)
        {
            // Can happen if the cluster model has changed since the cluster was saved
            LogFailedLoadFromSaveSlot(_logger, ex, saveSlot);
        }
    }

    private async Task<string?> LoadStoredClusterJson()
    {
        if (_disposed)
            return null;

        try
        {
            await _loadStoredClusterJsonCtsSemaphore.WaitAsync();
            try
            {
                await _loadStoredClusterJsonCts.CancelAsync();
                _loadStoredClusterJsonCts.Dispose();
                _loadStoredClusterJsonCts = new CancellationTokenSource();

                var cancellationToken = _loadStoredClusterJsonCts.Token;
                var (hasValueSucceeded, hasValue) = await TryInvoke<bool>("ViciOne.File.hasValue", cancellationToken, StartUpSaveSlot);

                if (!hasValueSucceeded || !hasValue)
                    return null;

                var (loadSucceeded, json) = await TryInvoke<string>("ViciOne.File.load", cancellationToken, StartUpSaveSlot);

                return loadSucceeded ? json : null;
            }
            finally
            {
                _loadStoredClusterJsonCtsSemaphore.Release();
            }
        }
        catch (OperationCanceledException ex)
        {
            // Expected during fast reload - suppress the exception
            LogLoadStoredClusterCanceled(_logger, ex);
        }
        catch (ObjectDisposedException) when (_disposed)
        {
            // Semaphore or other object already disposed, nothing we can do, return gracefully
        }

        return null;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {FnName} event handler.")]
    private static partial void LogEventHandlerException(ILogger<PersistenceService> logger, Exception ex, string fnName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to import cluster from {File}.")]
    private static partial void LogFailedImport(ILogger<PersistenceService> logger, Exception ex, string file);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load the cluster from save slot {SaveSlot}.")]
    private static partial void LogFailedLoadFromSaveSlot(ILogger<PersistenceService> logger, Exception ex, int saveSlot);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading the stored cluster was canceled.")]
    private static partial void LogLoadStoredClusterCanceled(ILogger<PersistenceService> logger, Exception ex);

    private Task OnSaveRequested(IClusterBuilder builder)
        => SaveToSaveSlot(StartUpSaveSlot, builder);

    /// <summary>
    /// Restores the cluster that was stored during the last session, or creates a new one if there is none.
    /// </summary>
    public async Task RestoreCluster()
        => await _clusterManagementService.InitCluster(await LoadStoredClusterJson());

    internal async Task SaveToSaveSlot(int saveSlot, IClusterBuilder? builder = null)
    {
        var json = SerializeCurrentCluster(builder);

        if (json is null)
            return;

        var (succeeded, saved) = await TryInvoke<bool>("ViciOne.File.save", CancellationToken.None, saveSlot, json);

        if (!succeeded)
            return;

        if (!saved)
            await InvokeSaveFailed();
    }

    private string? SerializeCurrentCluster(IClusterBuilder? builder = null)
    {
        var builderToSerialize = builder is null ? _clusterManagementService.Builder : builder;

        if (builderToSerialize is null)
            return null;

        _clusterManagementService.PrepareClusterSerialization();
        return ClusterSerializer.Serialize(builderToSerialize.Cluster);
    }

    private async Task<(bool Succeeded, T? Value)> TryInvoke<T>(string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        try
        {
            return (true, await _jsRuntime.InvokeAsync<T>(identifier, cancellationToken, args));
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            // Circuit already gone, JSRuntime already disposed or task already canceled
            return (false, default);
        }
    }

    private async Task<bool> TryInvokeVoid(string identifier, params object?[]? args)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(identifier, args);
            return true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            // Circuit already gone, JSRuntime already disposed or task already canceled
            return false;
        }
    }
}
