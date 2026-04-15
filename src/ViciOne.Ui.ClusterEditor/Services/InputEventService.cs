using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ViciOne.Ui.ClusterEditor.Services;

public class InputEventService
{
    public event Action<MouseEventArgs>? Clicked;
    public event Action<KeyboardEventArgs>? KeyDown;
    public event Action<PointerEventArgs>? PointerDown;
    public event Action<PointerEventArgs>? PointerLeave;
    public event Action<PointerEventArgs>? PointerMove;
    public event Action<PointerEventArgs>? PointerUp;

    public void InvokeClicked(MouseEventArgs e)
        => Clicked?.Invoke(e);

    [JSInvokable]
    public void InvokeKeyDown(KeyboardEventArgs e)
        => KeyDown?.Invoke(e);

    public void InvokePointerDown(PointerEventArgs e)
        => PointerDown?.Invoke(e);

    public void InvokePointerLeave(PointerEventArgs e)
        => PointerLeave?.Invoke(e);

    public void InvokePointerMove(PointerEventArgs e)
        => PointerMove?.Invoke(e);

    public void InvokePointerUp(PointerEventArgs e)
        => PointerUp?.Invoke(e);
}
