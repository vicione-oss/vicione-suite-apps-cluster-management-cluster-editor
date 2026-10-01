using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.Tables.Shared.Models;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.Models;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

public sealed partial class FbSettingsEditor : ComponentBase, IAsyncDisposable
{
    private const string AllColumnKey = "All";
    private const string CommentColumnKey = "Comment";
    private const string NameColumnKey = "Key";

    /// <summary>
    /// The cell the table last activated, or <see langword="null"/> once the request has been rendered.
    /// </summary>
    /// <remarks>
    /// A <see cref="Cell"/> opens its edit whenever its parameters are set with the flag on, so a request left
    /// standing would reopen the edit, and take the focus, on every later render.
    /// </remarks>
    private (string RowKey, string ColumnId)? _activatedCell;

    private IJSObjectReference? _cellModule;
    private readonly object _columnChooserToggleId = new();
    private bool _disposed;
    private readonly string _editTemplatesText = CompositeFormats.EditSomething(TechnicalTerms.TemplatePlural);
    private FilterState _filterState = FilterState.Empty;
    private bool _isFullscreen;
    private Dialog? _refDialog;
    private string? _searchText;
    private List<IGrouping<string, FbSetting>> _settings = [];
    private bool _visible;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private FullscreenService FullscreenService { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private ILogger<FbSettingsEditor> Logger { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private void CellActivated(string columnId, IGrouping<string, FbSetting> settingGroup)
        => _activatedCell = (settingGroup.Key, columnId);

    /// <summary>
    /// Stores the committed <paramref name="value"/> on the setting of <paramref name="fbName"/>, or on every
    /// setting of the row for the All column.
    /// </summary>
    /// <remarks>
    /// The <see cref="Cell"/> validates before it commits, so <paramref name="value"/> is always valid.
    /// </remarks>
    private void CellValueCommitted(string fbName, IGrouping<string, FbSetting> settingGroup, object? value)
    {
        if (fbName.Equals(AllColumnKey, StringComparison.Ordinal))
        {
            foreach (var fbSetting in settingGroup)
                fbSetting.Value = value;
        }
        else
        {
            GetSetting(fbName, settingGroup).Value = value;
        }

        // The values are mutated in place, so only a new list reference makes the table re-run the filter.
        // It costs the user their keyboard position, and with nothing filtering it can change neither the rows
        // nor their order, so it is paid only where it can matter.
        if (_filterState.Filters.Count > 0)
            _settings = [.. _settings];
    }

    public async ValueTask DisposeAsync()
    {
        FbSettingsEditorRequest.FbSettingsEditorRequested -= OnFbSettingsEditorRequested;
        FullscreenService.FullscreenStateChanged -= OnFullscreenStateChanged;

        _disposed = true;

        await DisposeModuleAsync();
    }

    private async ValueTask DisposeModuleAsync()
    {
        var module = _cellModule;
        _cellModule = null;

        if (module is null)
            return;

        await module.TryDisposeAsync(Logger);
    }

    private static object? GetCellValue(string fbName, IGrouping<string, FbSetting>? settingGroup)
    {
        if (settingGroup is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(fbName) || fbName.Equals(AllColumnKey, StringComparison.Ordinal))
        {
            var value = settingGroup.First().Value;
            if (settingGroup.All(s => s.Value is not null && s.Value.Equals(value)))
                return value;

            return null;
        }
        else
        {
            return settingGroup.First(g => g.FbName == fbName).Value;
        }
    }

    private static FbSetting GetSetting(string fbName, IGrouping<string, FbSetting> settingGroup)
    {
        if (fbName.Equals(AllColumnKey, StringComparison.Ordinal))
            return settingGroup.First();

        return settingGroup.First(g => g.FbName == fbName);
    }

    private static bool IsAnySettingModified(IGrouping<string, FbSetting> settingGroup)
        => settingGroup.Any(s => s.IsModified);

    private bool IsCellActivated(string columnId, IGrouping<string, FbSetting> settingGroup)
        => _activatedCell == (settingGroup.Key, columnId);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Imported once here rather than per cell, since every cell calls the same functions.
            var (_, module) = await JsRuntime.TryInvoke<IJSObjectReference>(
                Logger,
                "import",
                "./_content/ViciOne.Ui.ClusterEditor/Components/FbSettingsEditor/Cell.razor.js");

            _cellModule = module;

            if (_disposed)
                await DisposeModuleAsync();
        }

        // Rendering the request away is what lets the next Enter on the same cell arrive as a change rather
        // than as the flag the cell already carries.
        if (_activatedCell is not null)
        {
            _activatedCell = null;
            StateHasChanged();
        }
    }

    private async Task OnClose()
    {
        if (_refDialog is null)
            return;

        await _refDialog.CloseAsync();
    }

    private async Task OnDialogClosing()
    {
        if (_isFullscreen)
            await FullscreenService.SetFullscreen(false);

        _searchText = string.Empty;

        // A surviving filter would narrow the next session's rows with nothing on screen explaining why.
        _filterState = FilterState.Empty;
    }

    private async Task OnDialogOk()
    {
        foreach (var setting in
            from settingGroup in _settings
            from setting in settingGroup.Where(s => s.IsModified)
            select setting)
        {
            Datastore.Builder.Editors.Setting.SetValue(setting.Setting, setting.Value);
        }

        if (_refDialog is not null)
            await _refDialog.CloseAsync();
    }

    private async Task OnFbSettingsEditorRequested()
        => await Show();

    private async Task OnFullscreenButtonClicked()
        => await ToggleFullscreen();

    private async Task OnFullscreenStateChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        await InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        FbSettingsEditorRequest.FbSettingsEditorRequested += OnFbSettingsEditorRequested;
        FullscreenService.FullscreenStateChanged += OnFullscreenStateChanged;
    }

    private void SearchTextChanging(string? searchText)
    {
        _searchText = searchText;

        // The table reads a new instance as a command, so this may only run from an event: building one per
        // render would re-apply the filter on every render.
        _filterState = string.IsNullOrWhiteSpace(searchText)
            ? _filterState.WithoutGlobalFilter<FbSettingSearchFilter>()
            : _filterState.WithGlobalFilter(new FbSettingSearchFilter(searchText));
    }

    private async Task Show()
    {
        var fbCount = SelectionManager.SelectedFBs.Count;

        _settings = [.. SelectionManager.SelectedFBs
            .GetSettings(Datastore)
            .ToArray()
            .GroupBy(s => s.Name)
            .Where(sg => sg.Count() == fbCount)];

        if (_settings.Count > 0 && _refDialog is not null)
            await _refDialog.ShowAsync();
    }

    private async Task ToggleFullscreen()
    {
        if (_refDialog is not null)
        {
            _isFullscreen = !_isFullscreen;
            await FullscreenService.SetFullscreen(_isFullscreen);
            await InvokeAsync(StateHasChanged);
        }
    }
}
