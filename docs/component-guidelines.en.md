# Blazor component guideline


## Code behind pattern

Try to always use the "Code Behind" pattern when possible.

`/{Component}.razor`
``` razor
<div>
    @Reverse("argument")
</div>
```

`/{Component}.razor.cs`
``` csharp
namespace Namespace
{
    public sealed partial class Component : ComponentBase
    {
        private string Reverse(string argument) => string.Concat(argument.Reverse());
    }
}
```

## Attributes

### Ordering

The attribues of a component are ordered alphabetically within these groups
- Attributes starting with `@`
- Properties of the component
- Everything else

```razor
<TextBox @bind-Value="ConfiguringConnection.Name"
         AutoComplete="off"
         FocusLost="OnConnectionNameFocusOut"
         MaximumLength="@_nameMaxLength"
         ReadOnly="@(!_canEditConnectionName)" />
```

### Brackets  

Lambda functions should always be enclosed in brackts.

``` razor
<Component Attribute="@(() => Method(param))">
```

### Whitespace

Add one space before `/>`, but not before `>`.

### Child content

If a component has more than one attribute, write each attribute on a new line, indented to the column of the first attribute.
If the component has child content, add an empty line.

```
<button @onclick="OnButtonClicked"
        class="btn btn-success"
        type="button">

    <span class="mdi mdi-plus">
</button>
```

### DO's and DON'Ts

Don't use multiple `@` in one attribute.\
❌ `@bind-Text="@ConfiguringConnection.Name"`\
✔️ `@bind-Text="ConfiguringConnection.Name"`

Don't use unnecessary lambda functions.\
❌ `@onfocusout="(() => OnConnectionNameFocusOut())"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`

Use lambda functions to

- Pass paramenter\
✔️ `@onfocusout="(() => OnConnectionPropertyChanged(counter))`\
✔️ `@onfocusout="((args) => OnConnectionPropertyChanged(args))`
- Call asynchronous methods\
✔️ `@onfocusout="(async () => await OnConnectionPropertyChangedAsync())`

Don't 'hide' code in lambda functions, use the code behind file for this.\
❌ `@onfocusout="(() => { ValidateForm(); _connectionChanged = true; })"`\
✔️ `@onfocusout="OnConnectionNameFocusOut"`

## JavaScript interop

Never call `IJSRuntime.InvokeAsync` / `InvokeVoidAsync` or `IJSObjectReference.InvokeAsync` /
`InvokeVoidAsync` directly. Use the guarded extensions in
`ViciOne.Ui.ClusterEditor.Extensions` instead, and pass the component's or service's `ILogger`:

- `TryInvoke<TValue>(logger, identifier, …)` returns `(bool Success, TValue? Value)`
- `TryInvokeVoid(logger, identifier, …)` returns `bool`
- `TryDisposeAsync(logger)` replaces `IJSObjectReference.DisposeAsync()`

They translate the three "the JS target is gone" cases — `JSDisconnectedException` (the Blazor Server
circuit was lost), `ObjectDisposedException` (the runtime or module is already disposed) and a
cancellation that did not come from the caller's own token — into a `false` result plus a `Debug` log
entry, so a closed browser tab or a reconnect never crashes the editor.

Everything else still throws: a `JSException` means the JavaScript code itself failed and is a defect
we want to see, cancellation through the caller's own `CancellationToken` stays cooperative, and
argument-serialization or prerendering errors stay visible.

The overloads that take a `CancellationToken` only stop *waiting* when the token is cancelled; the token
is never passed to the framework. On Blazor Server the framework cancels a pending call from the token
callback, which can run on another thread (for example after `CancelAsync()`) while the circuit completes
the same call — that race throws inside `JSRuntime.EndInvokeJS` and tears down the circuit. The abandoned
call simply completes in the background. If the token is already cancelled, the call is not made.

Check `Success` before using the returned value and decide per call site what "the call did not happen"
means — a fallback value, skipping the operation, or propagating `null` to the caller.

❌ `await JsRuntime.InvokeVoidAsync("ViciOne.Element.focusByClass", cssClass);`\
✔️ `await JsRuntime.TryInvokeVoid(Logger, "ViciOne.Element.focusByClass", cssClass);`

For fire-and-forget calls from synchronous event handlers, wrap the guarded call in
`AsyncGuard.SafeFireAndForget` so an unguarded fault is still logged instead of being lost.\
❌ `_ = _jsRuntime.InvokeVoidAsync("ViciOne.NodeMove.end").AsTask();`\
✔️ `AsyncGuard.SafeFireAndForget(() => _jsRuntime.TryInvokeVoid(_logger, "ViciOne.NodeMove.end"), _logger);`
