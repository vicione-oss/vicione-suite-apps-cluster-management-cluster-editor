using System;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.MonochromeIcons.Core.Enums;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class FilterButton
{
    public MarkupString? Icon { get; set; }
    public bool IsActive { get; set; }
    public bool IsDisabled { get; set; }
    public MonochromeIconName? MonochromeIconName { get; set; }
    public Action? OnFilterClickedFn { get; set; }
    public string Title { get; set; } = string.Empty;

    public void InvokeOnFilterClickedFn()
    {
        if (IsDisabled)
            return;

        OnFilterClickedFn?.Invoke();
    }
}
