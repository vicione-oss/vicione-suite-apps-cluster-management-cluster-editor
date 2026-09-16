using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.Dialog.Components;
using ViciOne.Ui.Blazor.Components.Extensions;
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

#pragma warning disable IDE0052 // Remove unread private members
    private bool _closeOnEscape = true;
#pragma warning restore IDE0052 // Remove unread private members
    private bool _disposed;
    private readonly string _editTemplatesText = CompositeFormats.EditSomething(TechnicalTerms.TemplatePlural);
    private readonly Dictionary<string, object?> _initialEditValues = [];
    private bool _isEditmodeActive;
    private bool _isFullscreen;
    private IJSObjectReference? _jsModule;
    private bool _keepInitialValues;
    private Dialog? _refDialog;
    private IGrid? _refGrid;
    private DotNetObjectReference<FbSettingsEditor>? _refObject;
    private string? _searchText;
    private IEnumerable<IGrouping<string, FbSetting>> _settings = [];
    private string? _validationMessage;
    private bool _visible;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private FullscreenService FullscreenService { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private ILogger<FbSettingsEditor> Logger { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    public async ValueTask DisposeAsync()
    {
        FbSettingsEditorRequest.FbSettingsEditorRequested -= OnFbSettingsEditorRequested;
        FullscreenService.FullscreenStateChanged -= OnFullscreenStateChanged;

        _disposed = true;
        _refObject?.Dispose();

        await DisposeModuleAsync();
    }

    private async ValueTask DisposeModuleAsync()
    {
        var module = _jsModule;
        _jsModule = null;

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

    private static bool IsBooleanDataItem(IGrouping<string, FbSetting> settingGroup, out bool isNullable)
    {
        var setting = settingGroup.First();

        if (setting.SettingType != typeof(bool) && setting.SettingType != typeof(bool?))
        {
            isNullable = false;
            return false;
        }

        isNullable = setting.SettingType.IsNullableValueType() || setting.Value == null;
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        var (_, module) = await JsRuntime.TryInvoke<IJSObjectReference>(
            Logger,
            "import",
            "./_content/ViciOne.Ui.ClusterEditor/Components/FbSettingsEditor/FbSettingsEditor.razor.js");

        _jsModule = module;

        if (_disposed)
            await DisposeModuleAsync();
    }

    private void OnCheckedChanged(
        IGrouping<string, FbSetting> dataItem,
        bool? value,
        string fbName = "")
    {
        if (_initialEditValues.Count == 0)
        {
            _keepInitialValues = true;
            SetInitialValues(dataItem);
        }

        if (string.IsNullOrEmpty(fbName) || fbName.Equals(AllColumnKey, StringComparison.Ordinal))
        {
            foreach (var fbSetting in dataItem)
                fbSetting.Value = value;
        }
        else
        {
            dataItem.First(g => g.FbName == fbName).Value = value;
        }
    }

    private async Task OnClose()
    {
        if (_refDialog is null)
            return;

        await _refDialog.CloseAsync();
    }

    private static void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
        => e.EditModel = ((IGrouping<string, FbSetting>)e.DataItem).First();

    private async Task OnDialogClosing()
    {
        if (_jsModule is not null)
            await _jsModule.TryInvokeVoid(Logger, "removeEscEventListener");

        if (_isFullscreen)
            await FullscreenService.SetFullscreen(false);

        _searchText = string.Empty;
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

    private async Task OnDialogShowing()
    {
        _refObject ??= DotNetObjectReference.Create(this);

        if (_jsModule is not null)
            await _jsModule.TryInvokeVoid(Logger, "addEscEventListener", _refObject);
    }

    [JSInvokable]
    public async Task OnEscCaptured()
    {
        if (_isEditmodeActive && _refGrid is not null)
            await _refGrid.CancelEditAsync();
    }

    private async Task OnFbSettingsEditorRequested()
        => await Show();

    private async Task OnFullscreenButtonClicked()
        => await ToggleFullscreen();

    private async Task OnFullscreenStateChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        if (!_isEditmodeActive)
            _closeOnEscape = !isFullscreen;
        await InvokeAsync(StateHasChanged);
    }

    private void OnGridEditCancelling(GridEditCancelingEventArgs e)
    {
        if (_initialEditValues.Count > 0)
        {
            foreach (var setting in (IGrouping<string, FbSetting>)e.DataItem)
                setting.Value = _initialEditValues[setting.FbName];
        }

        _closeOnEscape = true;
        _isEditmodeActive = false;
    }

    private void OnGridEditModelSaving(GridEditModelSavingEventArgs e)
    {
        if (!string.IsNullOrEmpty(_validationMessage))
            e.Cancel = true;

        _closeOnEscape = true;
        _isEditmodeActive = false;
    }

    private void OnGridEditStart(GridEditStartEventArgs e)
    {
        _closeOnEscape = false;

        if (_keepInitialValues)
            _keepInitialValues = false;
        else
            _initialEditValues.Clear();

        _isEditmodeActive = true;
        _validationMessage = null;
    }

    protected override void OnInitialized()
    {
        FbSettingsEditorRequest.FbSettingsEditorRequested += OnFbSettingsEditorRequested;
        FullscreenService.FullscreenStateChanged += OnFullscreenStateChanged;
    }

    private void OnSearchTextChanging(string searchText)
        => _searchText = searchText;

    private void OnUnboundColumnData(GridUnboundColumnDataEventArgs e)
    {
        if (e.FieldName == AllColumnKey || _settings.Any(g => g.Any(s => s.FbName == e.FieldName)))
            e.Value = GetCellValue(e.FieldName, (IGrouping<string, FbSetting>)e.DataItem)?.ToString() ?? string.Empty;
    }

    private async Task OnValueBindingSet(
        string fbName,
        IGrouping<string, FbSetting> settingGroup,
        object? value)
    {
        try
        {
            var setting = GetSetting(fbName, settingGroup);

            Datastore.Builder.Editors.Setting.ValidateValue(setting.Setting, value);
            _validationMessage = null;
            if (fbName.Equals(AllColumnKey, StringComparison.Ordinal))
            {
                foreach (var fbSetting in settingGroup)
                    fbSetting.Value = value;
            }
            else
            {
                setting.Value = value;
            }
        }
        catch (Exception ex)
        {
            _validationMessage = ex.Message;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void SetInitialValues(IGrouping<string, FbSetting> dataItem)
    {
        _initialEditValues.Clear();
        foreach (var setting in dataItem)
            _initialEditValues[setting.FbName] = setting.Value;
    }

    private async Task Show()
    {
        var fbCount = SelectionManager.SelectedFBs.Count;

        _settings = SelectionManager.SelectedFBs
            .GetSettings(Datastore)
            .ToArray()
            .GroupBy(s => s.Name)
            .Where(sg => sg.Count() == fbCount);

        if (_settings.Any() && _refDialog is not null)
            await _refDialog.ShowAsync();
    }

    private async Task ToggleFullscreen()
    {
        if (_refDialog is not null)
        {
            _isFullscreen = !_isFullscreen;
            await FullscreenService.SetFullscreen(_isFullscreen);
            _closeOnEscape = !_isFullscreen;
            await InvokeAsync(StateHasChanged);
        }
    }
}
