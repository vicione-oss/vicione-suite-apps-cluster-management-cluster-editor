using System;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Sections.Information.Models;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Information.Components;

public sealed partial class InformationSection : ComponentBase, IDisposable
{
    private Statistic _statistic = new();

    [Inject] private StatisticService StatisticService { get; set; } = default!;

    public void Dispose()
        => StatisticService.StatisticChanged -= OnStatisticChanged;

    protected override void OnInitialized()
    {
        _statistic = StatisticService.GetStatistic();
        StatisticService.StatisticChanged += OnStatisticChanged;
    }

    private void OnStatisticChanged(Statistic statistic)
    {
        _statistic = statistic;
        InvokeAsync(StateHasChanged);
    }
}
