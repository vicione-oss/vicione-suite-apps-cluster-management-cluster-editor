using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Cluster.Builder.Abstractions;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer : IDisposable
{
    private CancellationTokenSource? _bufferCts;
    private bool _debounceRunning;
    private bool _disposed;
    private bool _manualBatchInProgress;

    private IClusterBuilder Builder { get; set; } = default!;

    private void AttachBuilderEvents()
    {
        AttachConnectorEvents();
        AttachContainerEvents();
        AttachDataflowEvents();
        AttachDataPortEvents();
        AttachEngineHostEvents();
        AttachFunctionBlockEvents();
        AttachLabelEvents();
        AttachNodeEvents();
        AttachNodeGroupEvents();
    }

    private void CancelBufferFlush()
    {
        var oldCts = Interlocked.Exchange(ref _bufferCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    private void DetachBuilderEvents()
    {
        if (Builder is null)
            return;

        DetachConnectorEvents();
        DetachContainerEvents();
        DetachDataflowEvents();
        DetachDataPortEvents();
        DetachEngineHostEvents();
        DetachFunctionBlockEvents();
        DetachLabelEvents();
        DetachNodeEvents();
        DetachNodeGroupEvents();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DetachBuilderEvents();
        CancelBufferFlush();
    }

    public void EndBatchOperation()
    {
        if (!_manualBatchInProgress)
            return;

        _manualBatchInProgress = false;
        ScheduleBufferFlush();
    }

    private void FireEvents()
    {
        FireConnectorEvents();
        FireContainerEvents();
        FireDataflowEvents();
        FireDataPortEvents();
        FireEngineHostEvents();
        FireFunctionBlockEvents();
        FireLabelEvents();
        FireNodeEvents();
        FireNodeGroupEvents();
    }

    private async Task FireEventsDebounced()
    {
        _debounceRunning = true;
        try
        {
            while (true)
            {
                var cts = Volatile.Read(ref _bufferCts);
                if (cts is null)
                    return;

                await Task.Delay(300, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (_disposed)
                    return;

                if (cts == Volatile.Read(ref _bufferCts))
                {
                    FireEvents();
                    return;
                }
            }
        }
        finally
        {
            _debounceRunning = false;
        }
    }

    public void ScheduleBufferFlush()
    {
        if (_manualBatchInProgress || _disposed)
            return;

        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _bufferCts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        if (!_debounceRunning)
            _ = FireEventsDebounced();
    }

    public void SetBuilder(IClusterBuilder builder)
    {
        DetachBuilderEvents();
        Builder = builder;
        AttachBuilderEvents();
    }

    public void StartBatchOpertation()
    {
        if (_manualBatchInProgress)
            return;

        _manualBatchInProgress = true;
        CancelBufferFlush();
    }
}
