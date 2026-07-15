using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static partial class AsyncGuard
{
    // internal (not private) so it is directly unit-testable and awaitable in tests.
    internal static async Task ExecuteSafely(Func<Task> operation, ILogger logger, string caller)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUnobservedAsyncOperation(logger, ex, caller);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unobserved exception in async handler {Caller}.")]
    internal static partial void LogUnobservedAsyncOperation(ILogger logger, Exception ex, string caller);

    /// <summary>Runs <paramref name="operation"/> without ever letting an exception escape;
    /// faults are logged. Returns void so it fits Action-based event handlers.</summary>
    internal static void SafeFireAndForget(
        Func<Task> operation,
        ILogger logger,
        [CallerMemberName] string caller = "")
        => _ = ExecuteSafely(operation, logger, caller);
}
