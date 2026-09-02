using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Shared.Settings.Services;

namespace BlazorWasm.Client.Layout;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Components cannot be internal")]
public sealed partial class MainLayout : IDisposable
{
    [Inject] private SettingsService SettingsService { get; set; } = default!;

    public void Dispose()
        => SettingsService.RefreshNeeded -= OnRefreshNeeded;

    protected override Task OnInitializedAsync()
    {
        SettingsService.RefreshNeeded += OnRefreshNeeded;
        return SettingsService.Load();
    }

    private Task OnRefreshNeeded()
        => InvokeAsync(StateHasChanged);
}
