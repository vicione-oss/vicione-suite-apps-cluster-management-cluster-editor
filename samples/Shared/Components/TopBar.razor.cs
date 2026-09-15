using Shared.Debugging.Components;
using Shared.Persistence.Components;
using Shared.Settings.Components;

namespace Shared.Components;

public partial class TopBar
{
    private DebugDialog? _refDebugDialog;
    private PersistenceDialog? _refPersistenceDialog;
    private SettingsDialog? _refSettingsDialog;

    private Task OnDebugClicked()
    {
        if (_refDebugDialog is not null)
            return _refDebugDialog.ShowDialog();

        return Task.CompletedTask;
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
