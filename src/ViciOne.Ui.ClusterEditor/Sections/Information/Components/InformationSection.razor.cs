using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.Information.Models;
using ViciOne.Ui.ClusterEditor.Sections.Information.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Information.Components;

public sealed partial class InformationSection : ComponentBase, IDisposable
{
    private string _editorVersion = "n/a";
    private Statistic _statistic = new();

    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private StatisticService StatisticService { get; set; } = default!;

    public void Dispose()
        => StatisticService.StatisticChanged -= OnStatisticChangedAsync;

    protected override async Task OnInitializedAsync()
    {
        _statistic = StatisticService.GetStatistic();
        StatisticService.StatisticChanged += OnStatisticChangedAsync;

        await RetrieveEditorVersionAsync();
    }

    private async void OnStatisticChangedAsync(Statistic statistic)
    {
        _statistic = statistic;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RetrieveEditorVersionAsync()
        => _editorVersion = await HttpClient!.TryRetrieveEditorVersionAsync(NavigationManager.BaseUri);
}
