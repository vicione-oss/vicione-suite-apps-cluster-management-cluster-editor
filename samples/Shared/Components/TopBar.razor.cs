using Shared.Persistence.Components;
using Shared.Settings.Components;

namespace Shared.Components;

public partial class TopBar
{
    private PersistenceDialog? _refPersistenceDialog;
    private SettingsDialog? _refSettingsDialog;

    private static void OnDebugClicked()
    {

    }

    private Task OnPersistenceClicked()
    {
        if (_refPersistenceDialog is not null)
            return _refPersistenceDialog.ShowDialog();

        return Task.CompletedTask;
    }

    private Task OnSettingsClicked()
    {
        if (_refSettingsDialog is not null)
            return _refSettingsDialog.ShowDialog();

        return Task.CompletedTask;
    }
}
