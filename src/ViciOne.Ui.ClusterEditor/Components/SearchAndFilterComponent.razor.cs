using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components;

public sealed partial class SearchAndFilterComponent : ComponentBase
{
    private readonly string _columnChooserId = "id" + Guid.NewGuid().ToString();
    private int _filterButtonGridColumnCount;
    private int _systemButtonGridColumnCount;

    [Parameter] public IEnumerable<FilterButton> FilterButtons { get; set; } = [];
    [Parameter] public bool GroupingButtonsEnabled { get; set; }
    [Parameter] public EventCallback OnCollapseAllGroups { get; set; }
    [Parameter] public EventCallback<string> OnColumnChooser { get; set; }
    [Parameter] public EventCallback OnExpandAllGroups { get; set; }
    [Parameter] public EventCallback<string> OnSearchTextChanged { get; set; }
    [Parameter] public bool ShowColumnChooser { get; set; } = true;
    [Parameter] public bool ShowGroupingButtons { get; set; } = true;
    [Parameter] public bool ShowHeading { get; set; } = true;

    private async Task OnColumnChooserAsync()
        => await OnColumnChooser.InvokeAsync("#" + _columnChooserId);

    protected override void OnParametersSet()
    {
        _filterButtonGridColumnCount = Math.Max(1, (FilterButtons.Count() * 2) - 1);
        _systemButtonGridColumnCount = Math.Max(0, ((ShowColumnChooser ? 3 : 2) * 2) - 1);
    }
}
