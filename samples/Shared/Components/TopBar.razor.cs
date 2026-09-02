using Microsoft.AspNetCore.Components;
using Shared.Settings.Services;

namespace Shared.Components;

public partial class TopBar
{
    [Inject] private SettingsDialogService SettingsDialogService { get; set; } = default!;

    private void OnDebugClicked()
    {

    }

    private void OnFileManagementClicked()
    {

    }

    private Task OnSettingsClicked()
        => SettingsDialogService.SetVisibility(true);
}
