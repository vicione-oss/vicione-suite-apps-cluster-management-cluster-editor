using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ViciOne.Ui.ClusterEditor.Services;

public class InputEventService
{
    public event Action<MouseEventArgs>? Clicked;
    public event Action<KeyboardEventArgs>? KeyDown;
    public event Action<MouseEventArgs>? PointerDown;
    public event Action<MouseEventArgs>? PointerMove;
    public event Action<MouseEventArgs>? PointerUp;

    public void InvokeClicked(MouseEventArgs args)
        => Clicked?.Invoke(args);

    [JSInvokable]
    public void InvokeKeyDown(KeyboardEventArgs args)
        => KeyDown?.Invoke(args);

    public void InvokePointerDown(MouseEventArgs args)
        => PointerDown?.Invoke(args);

    public void InvokePointerMove(MouseEventArgs args)
        => PointerMove?.Invoke(args);

    public void InvokePointerUp(MouseEventArgs args)
        => PointerUp?.Invoke(args);
}
