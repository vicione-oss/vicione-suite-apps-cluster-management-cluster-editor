using Microsoft.AspNetCore.Components;
using Shared.Settings.Extensions;
using Shared.Settings.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;

namespace Shared.Settings.Components;

public sealed partial class SettingsDialog : IDisposable
{
    private Dialog? _refDialog;
    private Models.Settings _settings = new();

    [Inject] private SettingsDialogService DialogService { get; set; } = default!;
    [Inject] private SettingsService SettingsService { get; set; } = default!;

    private Task CloseDialog()
    {
        if (_refDialog is not null)
            return _refDialog.CloseAsync();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DialogService.VisibilityChanged -= OnDialogServiceVisibilityChangedAsync;
        _refDialog = null;
    }

    private Task OnDialogCancel()
        => CloseDialog();

    private async Task OnDialogSave()
    {
        if (_settings.AreChanged(SettingsService.CurrentSettings))
            await SettingsService.SetCurrentSettings(_settings, true);

        await CloseDialog();
    }

    private async Task OnDialogServiceVisibilityChangedAsync()
    {
        if (_refDialog is null)
            return;

        if (DialogService.Visible)
            await _refDialog.ShowAsync();
        else
            await _refDialog.CloseAsync();
    }

    private void OnDialogShowing()
        => _settings = SettingsService.CurrentSettings.Clone();

    protected override void OnInitialized()
        => DialogService.VisibilityChanged += OnDialogServiceVisibilityChangedAsync;
}
