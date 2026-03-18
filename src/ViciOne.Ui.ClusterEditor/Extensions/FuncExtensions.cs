using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static partial class FuncExtensions
{
    internal static async Task InvokeEventAsync(this Func<Task>? eventHandler, ILogger? logger = null, string? eventName = null)
    {
        if (eventHandler is null)
            return;

        var tasks = eventHandler.GetInvocationList()
            .Cast<Func<Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    if (logger is not null)
                        LogFailedEventInvokation(logger, ex, string.IsNullOrWhiteSpace(eventName) ? "Unknown" : eventName);
                }
            });

        await Task.WhenAll(tasks);
    }

    internal static async Task InvokeEventAsync<T>(this Func<T, Task>? eventHandler, T arg, ILogger? logger = null, string? eventName = null)
    {
        if (eventHandler is null)
            return;

        var tasks = eventHandler.GetInvocationList()
            .Cast<Func<T, Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler(arg);
                }
                catch (Exception ex)
                {
                    if (logger is not null)
                        LogFailedEventInvokation(logger, ex, string.IsNullOrWhiteSpace(eventName) ? "Unknown" : eventName);
                }
            });

        await Task.WhenAll(tasks);
    }

    internal static async Task InvokeEventAsync<T1, T2, T3>(this Func<T1, T2, T3, Task>? eventHandler, T1 arg1, T2 arg2, T3 arg3, ILogger? logger = null, string? eventName = null)
    {
        if (eventHandler is null)
            return;

        var tasks = eventHandler.GetInvocationList()
            .Cast<Func<T1, T2, T3, Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler(arg1, arg2, arg3);
                }
                catch (Exception ex)
                {
                    if (logger is not null)
                        LogFailedEventInvokation(logger, ex, string.IsNullOrWhiteSpace(eventName) ? "Unknown" : eventName);
                }
            });

        await Task.WhenAll(tasks);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {NameOfEvent} event handler.")]
    internal static partial void LogFailedEventInvokation(ILogger logger, Exception ex, string nameOfEvent);
}
