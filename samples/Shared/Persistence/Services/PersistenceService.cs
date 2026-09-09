using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.ClusterSerialization;
using Shared.Services;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Persistence.Services;

/// <summary>
/// Owns everything that is needed to persist a cluster: the browser storage, the file download and upload
/// and the serialization. The boundary towards <see cref="IndexService"/> is the cluster JSON.
/// </summary>
public sealed partial class PersistenceService : IAsyncDisposable
{
    private const int EmptySaveSlotSize = -1;
    private const int MaxUploadFileSize = 1024 * 1024 * 1024;
    internal const int SaveSlotCount = 3;
    internal const int StartUpSaveSlot = SaveSlotCount + 1;

    private static readonly int[] s_allSaveSlots = [.. Enumerable.Range(1, StartUpSaveSlot)];

    private readonly IClusterEditorManagement _clusterEditorManagement;
    private bool _disposed;
    private readonly IndexService _indexService;
    private readonly IJSRuntime _jsRuntime;
    private CancellationTokenSource _loadStoredClusterJsonCts = new();
    private readonly SemaphoreSlim _loadStoredClusterJsonCtsSemaphore = new(1);
    private readonly ILogger<PersistenceService> _logger;
    private readonly Dictionary<int, int> _saveSlotSizes = s_allSaveSlots.ToDictionary(saveSlot => saveSlot, _ => EmptySaveSlotSize);

    public event Func<Task>? SaveFailed;
    internal event Func<Task>? SaveSlotSizesChanged;

    public PersistenceService(
        IClusterEditorManagement clusterEditorManagement,
        IndexService indexService,
        IJSRuntime jsRuntime,
        ILogger<PersistenceService> logger)
    {
        _clusterEditorManagement = clusterEditorManagement;
        _indexService = indexService;
        _jsRuntime = jsRuntime;
        _logger = logger;

        _clusterEditorManagement.SaveRequested += OnSaveRequested;
        _indexService.ClusterLoaded += OnClusterLoaded;
    }

    internal async Task ClearSaveSlot(int saveSlot)
    {
        if (!await TryInvokeVoid("ViciOne.File.clear", saveSlot))
            return;

        await RefreshSaveSlotSizes();
    }

    internal Task ClearStartUpCluster()
        => ClearSaveSlot(StartUpSaveSlot);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, true, false))
            return;

        _clusterEditorManagement.SaveRequested -= OnSaveRequested;
        _indexService.ClusterLoaded -= OnClusterLoaded;

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

    internal int GetSaveSlotSize(int saveSlot)
        => _saveSlotSizes.TryGetValue(saveSlot, out var saveSlotSize) ? saveSlotSize : EmptySaveSlotSize;

    internal async Task ImportCluster(IBrowserFile file)
    {
        try
        {
            await using MemoryStream ms = new();
            await file.OpenReadStream(MaxUploadFileSize).CopyToAsync(ms);
            var json = Encoding.UTF8.GetString(ms.ToArray());

            await _indexService.LoadClusterJson(json);
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

    private async Task InvokeSaveSlotSizesChanged()
    {
        if (SaveSlotSizesChanged is null)
            return;

        var tasks = SaveSlotSizesChanged.GetInvocationList()
            .Cast<Func<Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    LogEventHandlerException(_logger, ex, $"{nameof(PersistenceService)}.{nameof(SaveSlotSizesChanged)}");
                }
            });

        await Task.WhenAll(tasks);
    }

    internal async Task LoadFromSaveSlot(int saveSlot)
    {
        var (succeeded, json) = await TryInvoke<string>("ViciOne.File.load", CancellationToken.None, saveSlot);

        if (!succeeded || string.IsNullOrEmpty(json))
            return;

        await _indexService.LoadClusterJson(json);
    }

    internal Task LoadStartUpCluster()
        => LoadFromSaveSlot(StartUpSaveSlot);

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading the stored cluster was canceled.")]
    private static partial void LogLoadStoredClusterCanceled(ILogger<PersistenceService> logger, Exception ex);

    private Task OnClusterLoaded(IClusterBuilder builder)
        => _indexService.LoadCluster(builder);

    private Task OnSaveRequested(IClusterBuilder builder)
        => SaveToSaveSlot(StartUpSaveSlot);

    internal async Task RefreshSaveSlotSizes()
    {
        var (succeeded, saveSlotSizes) = await TryInvoke<int[]>("ViciOne.File.getSizes", CancellationToken.None, s_allSaveSlots);

        if (!succeeded || saveSlotSizes is null || saveSlotSizes.Length != s_allSaveSlots.Length)
            return;

        for (var i = 0; i < s_allSaveSlots.Length; i++)
            _saveSlotSizes[s_allSaveSlots[i]] = saveSlotSizes[i];

        await InvokeSaveSlotSizesChanged();
    }

    /// <summary>
    /// Restores the cluster that was stored during the last session, or creates a new one if there is none.
    /// </summary>
    public async Task RestoreCluster()
        => await _indexService.InitCluster(await LoadStoredClusterJson());

    internal Task SaveStartUpCluster()
        => SaveToSaveSlot(StartUpSaveSlot);

    internal async Task SaveToSaveSlot(int saveSlot)
    {
        var json = SerializeCurrentCluster();

        if (json is null)
            return;

        var (succeeded, saved) = await TryInvoke<bool>("ViciOne.File.save", CancellationToken.None, saveSlot, json);

        if (!succeeded)
            return;

        if (!saved)
        {
            await InvokeSaveFailed();
            return;
        }

        await RefreshSaveSlotSizes();
    }

    private string? SerializeCurrentCluster()
    {
        if (_indexService.Builder is null)
            return null;

        _indexService.PrepareClusterSerialization();
        return ClusterSerializer.Serialize(_indexService.Builder.Cluster);
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
