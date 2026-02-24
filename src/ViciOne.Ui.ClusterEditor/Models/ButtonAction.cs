using System;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class ButtonAction
{
    public required Action Action { get; init; }
    public Func<bool> ActiveCondition { get; set; } = () => false;
    public Func<bool> DisabledCondition { get; set; } = () => false;
    public string IconClass { get; init; } = string.Empty;
    public string Tooltip { get; init; } = string.Empty;
}
