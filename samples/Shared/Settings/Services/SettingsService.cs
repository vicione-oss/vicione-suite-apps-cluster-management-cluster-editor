using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.Settings.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

namespace Shared.Settings.Services;

public sealed partial class SettingsService(IJSRuntime jsRuntime, NavigationManager navigationManager, ISettingsService settingsService, ILogger<SettingsService> logger)
{
    internal Models.Settings CurrentSettings { get; private set; } = new();
    public bool ShowDefaultContextMenu => CurrentSettings.ShowDefaultContextMenu;
    internal static IEnumerable<string> SupportedCultureNames => [new("en-US"), new("de-DE")];

    public event Func<Task>? RefreshNeeded;

    private async Task InvokeRefreshNeeded()
    {
        if (RefreshNeeded is null)
            return;

        var tasks = RefreshNeeded.GetInvocationList()
            .Cast<Func<Task>>()
            .Select(async handler =>
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    LogEventHandlerException(logger, ex, $"{nameof(SettingsService)}.{nameof(RefreshNeeded)}");
                }
            });

        await Task.WhenAll(tasks);
    }

    public async Task Load()
    {
        try
        {
            var result = await jsRuntime.InvokeAsync<string>("ViciOne.Settings.getCurrentSettings");

            if (!string.IsNullOrEmpty(result))
            {
                var settings = JsonSerializer.Deserialize<Models.Settings>(result);
                if (settings is not null)
                    CurrentSettings = settings;
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            // Circuit already gone, JSRuntime already disposed or task already canceled
            return;
        }

        if (CultureInfo.CurrentCulture.Name != CurrentSettings.CurrentCultureName)
            UpdateCulture(CurrentSettings.CurrentCultureName);

        settingsService.SetGridMode(CurrentSettings.GetGridMode());
        settingsService.SetMinimapNodeColoring(CurrentSettings.MinimapNodeColoring);
        settingsService.SetNodeAlignmentBorder(CurrentSettings.NodeAlignmentBorder);
        settingsService.SetPanBehavior(CurrentSettings.PanBehavior);
        settingsService.SetSimplifiedView(CurrentSettings.SimplifiedView);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception in {FnName} event handler.")]
    private static partial void LogEventHandlerException(ILogger<SettingsService> logger, Exception ex, string fnName);

    internal async Task SaveCurrentSettings()
    {
        var serializedSettings = JsonSerializer.Serialize(CurrentSettings);
        await jsRuntime.InvokeVoidAsync("ViciOne.Settings.setCurrentSettings", serializedSettings);
    }

    internal async Task SetCurrentSettings(Models.Settings settings, bool doSave = true)
    {
        if (!settings.AreChanged(CurrentSettings))
            return;

        if (settings.CurrentCultureName != CurrentSettings.CurrentCultureName)
            UpdateCulture(settings.CurrentCultureName);

        if (settings.ShowDefaultContextMenu != CurrentSettings.ShowDefaultContextMenu)
            await InvokeRefreshNeeded();

        if (settings.GridMode != CurrentSettings.GridMode)
            settingsService.SetGridMode(settings.GetGridMode());

        if (settings.MinimapNodeColoring != CurrentSettings.MinimapNodeColoring)
            settingsService.SetMinimapNodeColoring(settings.MinimapNodeColoring);

        if (settings.NodeAlignmentBorder != CurrentSettings.NodeAlignmentBorder)
            settingsService.SetNodeAlignmentBorder(settings.NodeAlignmentBorder);

        if (settings.PanBehavior != CurrentSettings.PanBehavior)
            settingsService.SetPanBehavior(settings.PanBehavior);

        if (settings.SimplifiedView != CurrentSettings.SimplifiedView)
            settingsService.SetSimplifiedView(settings.SimplifiedView);

        CurrentSettings = settings;

        if (doSave)
            await SaveCurrentSettings();
    }

    private void UpdateCulture(string newCulture)
    {
        CultureInfo cultureInfo = new(newCulture);
        CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
        CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        navigationManager.NavigateTo(navigationManager.Uri.ToString(), true);
    }
}
