using System;
using System.Timers;
using ViciOne.Cluster.Builder;

namespace ViciOne.Ui.ClusterEditor.Services;

public sealed partial class ClusterBuilderEventBuffer : IDisposable
{
    private readonly Timer _bufferTimer = new()
    {
        AutoReset = false,
        Enabled = false,
        Interval = 300,
    };
    private bool _manualBatchInProgress;

    private ClusterBuilder Builder { get; set; } = default!;

    public ClusterBuilderEventBuffer()
        => _bufferTimer.Elapsed += OnBufferTimerElapsed;

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
        DetachBuilderEvents();

        _bufferTimer.Elapsed -= OnBufferTimerElapsed;
        _bufferTimer.Dispose();
    }

    public void EndBatchOperation()
    {
        if (!_manualBatchInProgress)
            return;

        _manualBatchInProgress = false;
        StartBufferTimer();
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

    private void OnBufferTimerElapsed(object? s, ElapsedEventArgs e)
    {
        try
        {
            _bufferTimer.Stop();
        }
        catch (ObjectDisposedException)
        {
            // Timer was already disposed, ignore
        }

        FireEvents();
    }

    public void SetBuilder(ClusterBuilder builder)
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

        try
        {
            _bufferTimer.Stop();
        }
        catch (ObjectDisposedException)
        {
            // Timer was already disposed, ignore
        }
    }

    public void StartBufferTimer()
    {
        if (_manualBatchInProgress)
            return;

        try
        {
            _bufferTimer.Stop();
            _bufferTimer.Start();
        }
        catch (ObjectDisposedException)
        {
            // Timer was already disposed, ignore
        }
    }
}
