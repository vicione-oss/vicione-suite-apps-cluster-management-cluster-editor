using Microsoft.AspNetCore.Components;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarSearchbox;

/// <summary>
/// Search box to be placed inside a toolbar group of a dialog header.
/// </summary>
public sealed partial class ToolbarSearchbox
{
    [Parameter] public string? Text { get; set; }
    [Parameter] public EventCallback<string?> TextChanging { get; set; }
}
