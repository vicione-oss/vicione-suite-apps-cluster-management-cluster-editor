using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Shared.Settings.Services;

namespace BlazorWasm.Client.Layout;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Components cannot be internal")]
public sealed partial class MainLayout : IAsyncDisposable
{
    [Inject] private SettingsService SettingsService { get; set; } = default!;

    public async ValueTask DisposeAsync()
        => await SettingsService.UnregisterContextMenuHandler();

    protected override async Task OnInitializedAsync()
    {
        await SettingsService.Load();
        await SettingsService.RegisterContextMenuHandler();
    }
}
