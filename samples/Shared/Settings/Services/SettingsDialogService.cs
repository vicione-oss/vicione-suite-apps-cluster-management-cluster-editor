using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Shared.Settings.Extensions;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.ClusterEditor.Models;

namespace Shared.Settings.Services;

[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated through DI and only used in sample app")]
internal sealed partial class SettingsDialogService(ILogger<SettingsDialogService> logger)
{
    public static List<ComboBoxItem<int, string>> GridModeComboBoxItems => [..
        Enum.GetValues<GridMode>().Select(gm => new ComboBoxItem<int, string>
        {
            Text = gm.ToLocalizedString(),
            Value = (int)gm,
        })];
    public bool Visible { get; private set; }

    public event Func<Task>? VisibilityChanged;

    private async Task InvokeVisibilityChanged()
    {
        if (VisibilityChanged is null)
            return;

        var tasks = VisibilityChanged.GetInvocationList()
            .Cast<Func<Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    LogEventHandlerException(logger, ex, $"{nameof(SettingsDialogService)}.{nameof(VisibilityChanged)}");
                }
            });

        await Task.WhenAll(tasks);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {FnName} event handler.")]
    private static partial void LogEventHandlerException(ILogger<SettingsDialogService> logger, Exception ex, string fnName);

    public async Task SetVisibility(bool visible)
    {
        if (visible != Visible)
        {
            Visible = visible;

            await InvokeVisibilityChanged();
        }
    }
}
