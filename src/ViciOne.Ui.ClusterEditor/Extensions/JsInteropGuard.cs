using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ViciOne.Ui.ClusterEditor.Extensions;

/// <summary>
/// Single place where the "the JavaScript call could not complete" exceptions are caught and logged.
/// Shared by <see cref="IJSRuntimeExtensions"/> and <see cref="IJSObjectReferenceExtensions"/>.
/// </summary>
internal static partial class JsInteropGuard
{
    internal const string DisposedEventName = "JsInteropDisposed";
    internal const string UnavailableEventName = "JsInteropUnavailable";

    [LoggerMessage(
        EventName = DisposedEventName,
        Level = LogLevel.Debug,
        Message = "JavaScript call {Identifier} was made against a disposed object.")]
    internal static partial void LogDisposed(ILogger logger, Exception ex, string identifier);

    [LoggerMessage(
        EventName = UnavailableEventName,
        Level = LogLevel.Debug,
        Message = "JavaScript call {Identifier} did not complete because the JS target is unavailable.")]
    internal static partial void LogUnavailable(ILogger logger, Exception ex, string identifier);

    /// <summary>
    /// Runs <paramref name="invoke"/> and turns the three "the JS target is gone" exceptions into a
    /// <c>false</c> result plus a log entry. Every other exception — <see cref="JSException"/> raised by
    /// the JavaScript code itself, argument serialization, prerendering, test doubles — still escapes as
    /// a faulted <see cref="Task"/>, because those signal a defect rather than a vanished target.
    /// </summary>
    /// <remarks>
    /// <paramref name="cancellationToken"/> is deliberately <em>not</em> handed to the framework. Blazor
    /// Server cancels a pending interop call from the token callback, which can run on another thread (for
    /// example after <see cref="CancellationTokenSource.CancelAsync"/>) while the circuit completes the same
    /// call with the JavaScript reply. That race throws <see cref="InvalidOperationException"/> inside
    /// <c>JSRuntime.EndInvokeJS</c> and tears down the circuit. Instead, only the wait is cancelled: the
    /// caller gets an <see cref="OperationCanceledException"/>, and the abandoned call completes normally.
    /// </remarks>
    internal static async Task<(bool Success, TValue? Value)> RunGuarded<TValue>(
        Func<ValueTask<TValue>> invoke,
        ILogger logger,
        string identifier,
        CancellationToken cancellationToken)
    {
        Task<TValue>? pending = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calling invoke() inside the try is deliberate: RemoteJSRuntime throws
            // JSDisconnectedException synchronously once the circuit is gone.
            pending = invoke().AsTask();
            var value = await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
            return (true, value);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Canceled by the JS side or by the interop timeout, not by our caller.
            LogUnavailable(logger, ex, identifier);
        }
        catch (OperationCanceledException) when (pending is { IsCompleted: false })
        {
            // Our caller stopped waiting. Observe the abandoned call so a later fault is not reported as unobserved.
            _ = pending.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            throw;
        }
        catch (JSDisconnectedException ex)
        {
            LogUnavailable(logger, ex, identifier);
        }
        catch (ObjectDisposedException ex)
        {
            LogDisposed(logger, ex, identifier);
        }

        return (false, default);
    }

    /// <summary>
    /// Runs <paramref name="invoke"/> and turns the three "the JS target is gone" exceptions into a
    /// <c>false</c> result plus a log entry. See the generic overload for what is not caught.
    /// </summary>
    internal static async Task<bool> RunGuarded(
        Func<ValueTask> invoke,
        ILogger logger,
        string identifier,
        CancellationToken cancellationToken)
    {
        var (success, _) = await RunGuarded<object?>(
            async () =>
            {
                await invoke().ConfigureAwait(false);
                return null;
            },
            logger,
            identifier,
            cancellationToken).ConfigureAwait(false);

        return success;
    }
}
