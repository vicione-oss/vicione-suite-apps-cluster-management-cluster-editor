using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class SearchAndFilterComponent : ComponentBase
{
    private int _filterButtonGridColumnCount;
    private int _systemButtonGridColumnCount;

    /// <summary>
    /// The column chooser shown beside the grouping buttons. The section supplies it, because the toggle is
    /// bound to its table by an id the section owns.
    /// </summary>
    [Parameter] public RenderFragment? ColumnChooserSlot { get; set; }

    [Parameter] public IReadOnlyList<FilterButton> FilterButtons { get; set; } = [];
    [Parameter] public bool GroupingButtonsEnabled { get; set; }
    [Parameter] public EventCallback OnCollapseAllGroups { get; set; }
    [Parameter] public EventCallback OnExpandAllGroups { get; set; }
    [Parameter] public bool SearchEnabled { get; set; } = true;
    [Parameter] public string? SearchText { get; set; }
    [Parameter] public EventCallback<string?> SearchTextChanged { get; set; }
    [Parameter] public bool ShowGroupingButtons { get; set; } = true;
    [Parameter] public bool ShowHeading { get; set; } = true;

    protected override void OnParametersSet()
    {
        _filterButtonGridColumnCount = Math.Max(1, (FilterButtons.Count * 2) - 1);
        _systemButtonGridColumnCount = Math.Max(0, ((ColumnChooserSlot is not null ? 3 : 2) * 2) - 1);
    }
}
