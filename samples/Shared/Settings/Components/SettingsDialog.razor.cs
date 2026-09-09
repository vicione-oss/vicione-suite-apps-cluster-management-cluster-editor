using Microsoft.AspNetCore.Components;
using Shared.Settings.Extensions;
using Shared.Settings.Services;
using ViciOne.Ui.Blazor.Components.Dialog.Components;

namespace Shared.Settings.Components;

public sealed partial class SettingsDialog
{
    private Dialog? _refDialog;
    private Models.Settings _settings = new();

    [Inject] private SettingsService SettingsService { get; set; } = default!;

    private Task CloseDialog()
    {
        if (_refDialog is not null)
            return _refDialog.CloseAsync();

        return Task.CompletedTask;
    }

    private async Task OnDialogCancel()
    {
        await SettingsService.RestoreLastSavedShowDefaultContextMenu();
        await CloseDialog();
    }

    private async Task OnDialogSave()
    {
        if (_settings.AreChanged(SettingsService.CurrentSettings))
            await SettingsService.SetCurrentSettings(_settings, true);

        await CloseDialog();
    }

    private void OnDialogShowing()
        => _settings = SettingsService.CurrentSettings.Clone();

    private async Task OnShowDefaultContextMenuChanged(bool showDefaultContextMenu)
    {
        _settings.ShowDefaultContextMenu = showDefaultContextMenu;
        await SettingsService.SetShowDefaultContextMenu(showDefaultContextMenu);
    }

    internal Task ShowDialog()
    {
        if (_refDialog is not null)
            return _refDialog.ShowAsync();

        return Task.CompletedTask;
    }
}
