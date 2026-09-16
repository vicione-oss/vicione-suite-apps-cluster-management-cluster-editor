using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class IJSObjectReferenceExtensions
{
    /// <summary>
    /// Releases the JS object reference and reports whether disposal completed instead of throwing when
    /// the JS target is unavailable. See <see cref="JsInteropGuard.RunGuarded{TValue}"/> for what is caught.
    /// </summary>
    /// <remarks>
    /// Disposal is not an invoke, so the identifier that is logged is the calling member rather than a
    /// JavaScript function name.
    /// </remarks>
    internal static Task<bool> TryDisposeAsync(
        this IJSObjectReference jsObjectReference,
        ILogger logger,
        [CallerMemberName] string caller = "")
        => JsInteropGuard.RunGuarded(
            jsObjectReference.DisposeAsync,
            logger,
            caller,
            CancellationToken.None);

    /// <summary>
    /// Invokes <paramref name="identifier"/> and reports whether the call completed instead of throwing
    /// when the JS target is unavailable. See <see cref="JsInteropGuard.RunGuarded{TValue}"/> for what is caught.
    /// </summary>
    internal static Task<(bool Success, TValue? Value)> TryInvoke<TValue>(
        this IJSObjectReference jsObjectReference,
        ILogger logger,
        string identifier,
        params object?[]? args)
        => JsInteropGuard.RunGuarded(
            () => jsObjectReference.InvokeAsync<TValue>(identifier, args),
            logger,
            identifier,
            CancellationToken.None);

    /// <summary>
    /// Invokes <paramref name="identifier"/> and reports whether the call completed instead of throwing
    /// when the JS target is unavailable. See <see cref="JsInteropGuard.RunGuarded{TValue}"/> for what is caught.
    /// </summary>
    /// <remarks>
    /// Two binding hazards, both shared with the framework's own <c>JSObjectReferenceExtensions</c>:
    /// a <see cref="CancellationToken"/> meant as a JavaScript <em>argument</em> binds to
    /// <paramref name="cancellationToken"/> and is never passed to JavaScript; and because
    /// <paramref name="args"/> is a <c>params</c> array, passing a single array spreads it into one
    /// JavaScript argument per element rather than passing it as one array argument.
    /// </remarks>
    internal static Task<(bool Success, TValue? Value)> TryInvoke<TValue>(
        this IJSObjectReference jsObjectReference,
        ILogger logger,
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
        => JsInteropGuard.RunGuarded(
            () => jsObjectReference.InvokeAsync<TValue>(identifier, args),
            logger,
            identifier,
            cancellationToken);

    /// <summary>
    /// Invokes <paramref name="identifier"/> and reports whether the call completed instead of throwing
    /// when the JS target is unavailable. See <see cref="JsInteropGuard.RunGuarded{TValue}"/> for what is caught.
    /// </summary>
    internal static async Task<bool> TryInvokeVoid(
        this IJSObjectReference jsObjectReference,
        ILogger logger,
        string identifier,
        params object?[]? args)
    {
        var (success, _) = await JsInteropGuard.RunGuarded(
            () => jsObjectReference.InvokeAsync<IJSVoidResult>(identifier, args),
            logger,
            identifier,
            CancellationToken.None).ConfigureAwait(false);

        return success;
    }

    /// <summary>
    /// Invokes <paramref name="identifier"/> and reports whether the call completed instead of throwing
    /// when the JS target is unavailable. See <see cref="JsInteropGuard.RunGuarded{TValue}"/> for what is caught.
    /// </summary>
    /// <remarks>
    /// Two binding hazards, both shared with the framework's own <c>JSObjectReferenceExtensions</c>:
    /// a <see cref="CancellationToken"/> meant as a JavaScript <em>argument</em> binds to
    /// <paramref name="cancellationToken"/> and is never passed to JavaScript; and because
    /// <paramref name="args"/> is a <c>params</c> array, passing a single array spreads it into one
    /// JavaScript argument per element rather than passing it as one array argument.
    /// </remarks>
    internal static async Task<bool> TryInvokeVoid(
        this IJSObjectReference jsObjectReference,
        ILogger logger,
        string identifier,
        CancellationToken cancellationToken,
        params object?[]? args)
    {
        var (success, _) = await JsInteropGuard.RunGuarded(
            () => jsObjectReference.InvokeAsync<IJSVoidResult>(identifier, args),
            logger,
            identifier,
            cancellationToken).ConfigureAwait(false);

        return success;
    }
}
