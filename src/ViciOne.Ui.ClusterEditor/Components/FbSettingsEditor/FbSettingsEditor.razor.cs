using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ViciOne.Ui.Blazor.Components.Extensions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Localization;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.Shared.Dx.Components;
using TechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

public sealed partial class FbSettingsEditor : ComponentBase, IAsyncDisposable
{
    private const string AllColumnKey = "All";
    private const string CommentColumnKey = "Comment";
    private const string NameColumnKey = "Key";

    private bool _closeOnEscape = true;
    private readonly string _columnChooserId = "id" + Guid.NewGuid();
    private readonly string _editTemplatesText = CompositeFormats.EditSomething(TechnicalTerms.TemplatePlural);
    private readonly Dictionary<string, object?> _initialEditValues = [];
    private bool _isEditmodeActive;
    private bool _isFullscreen;
    private IJSObjectReference? _jsModule;
    private bool _keepInitialValues;
    private DxDialog? _refDialog;
    private IGrid? _refGrid;
    private DotNetObjectReference<FbSettingsEditor>? _refObject;
    private IEnumerable<IGrouping<string, FbSetting>> _settings = [];
#pragma warning disable CS0649 // Field 'FbSettingsEditor._templateCount' is never assigned to, and will always have its default value 0
    // ToDo: Warnung deaktiviert, da Templates für FbSettings noch nicht implementiert sind und daher der Count immer 0 sein soll
    private readonly int _templateCount;
#pragma warning restore CS0649 
    private string? _validationMessage;

    [Inject] private Datastore Datastore { get; set; } = default!;
    [Inject] private IFbSettingsEditorRequest FbSettingsEditorRequest { get; set; } = default!;
    [Inject] private FullscreenService FullscreenService { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;
    [Inject] private SelectionManager SelectionManager { get; set; } = default!;

    private string? SearchText { get; set; }

    public async ValueTask DisposeAsync()
    {
        FbSettingsEditorRequest.FbSettingsEditorRequested -= OnFbSettingsEditorRequestedAsync;
        FullscreenService.FullscreenStateChanged -= OnFullscreenStateChanged;

        _refObject?.Dispose();

        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.DisposeAsync();
                _jsModule = null;
            }
            catch (JSDisconnectedException)
            {
                // JSDisconnectedException is trapped during module disposal
                // in case Blazor's SignalR circuit is lost.
            }
        }
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
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/ViciOne.Ui.ClusterEditor/Components/FbSettingsEditor/FbSettingsEditor.razor.js");
        }
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

    private async Task OnCloseAsync()
    {
        if (_refDialog is null)
            return;

        await _refDialog.CloseAsync();
    }

    private static void OnCustomizeEditModel(GridCustomizeEditModelEventArgs e)
        => e.EditModel = ((IGrouping<string, FbSetting>)e.DataItem).First();

    private async void OnDialogClosingAsync()
    {
        if (_jsModule is not null)
            await _jsModule.InvokeVoidAsync("removeEscEventListener");

        if (_isFullscreen)
            await FullscreenService.SetFullscreenAsync(false);

        SearchText = string.Empty;
    }

    private async Task OnDialogOkAsync()
    {
        foreach (var setting in
            from settingGroup in _settings
            from setting in settingGroup.Where(s => s.IsModified)
            select setting)
        {
            Datastore.Builder.Editors.Setting.SetValue(setting.Setting, setting.Value);
        }

        await _refDialog!.CloseAsync();
    }

    private async void OnDialogShowingAsync()
    {
        _refObject ??= DotNetObjectReference.Create(this);

        if (_jsModule is not null)
            await _jsModule.InvokeVoidAsync("addEscEventListener", _refObject);
    }

    [JSInvokable]
    public async Task OnEscCapturedAsync()
    {
        if (_isEditmodeActive && _refGrid is not null)
            await _refGrid.CancelEditAsync();
    }

    private async Task OnFbSettingsEditorRequestedAsync()
        => await ShowAsync();

    private async Task OnFullscreenButtonClickedAsync()
        => await ToggleFullscreenAsync();

    private void OnFullscreenStateChanged(bool isFullscreen)
    {
        _isFullscreen = isFullscreen;
        if (!_isEditmodeActive)
            _closeOnEscape = !isFullscreen;
        InvokeAsync(StateHasChanged);
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
        FbSettingsEditorRequest.FbSettingsEditorRequested += OnFbSettingsEditorRequestedAsync;
        FullscreenService.FullscreenStateChanged += OnFullscreenStateChanged;
    }

    private void OnSearchTextChanged(string searchText)
        => SearchText = searchText;

    private void OnShowColumnChooser()
        => _refGrid?.ShowColumnChooser(new DialogDisplayOptions("#" + _columnChooserId, HorizontalAlignment.Right, VerticalAlignment.Top));

    private void OnUnboundColumnData(GridUnboundColumnDataEventArgs e)
    {
        if (e.FieldName == AllColumnKey || _settings.Any(g => g.Any(s => s.FbName == e.FieldName)))
            e.Value = GetCellValue(e.FieldName, (IGrouping<string, FbSetting>)e.DataItem)?.ToString() ?? string.Empty;
    }

    private async Task OnValueBindingSetAsync(
        string fbName,
        IGrouping<string, FbSetting> settingGroup,
        object? value)
    {
        if (_refGrid is null || !_refGrid.IsEditing())
            return;

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

    private async Task ShowAsync()
    {
        var fbCount = SelectionManager.SelectedFBs.Count();

        _settings = SelectionManager.SelectedFBs
            .GetSettings(Datastore)
            .ToArray()
            .GroupBy(s => s.Name)
            .Where(sg => sg.Count() == fbCount);

        if (_settings.Any())
            await _refDialog!.OpenAsync();
    }

    private async Task ToggleFullscreenAsync()
    {
        if (_refDialog is not null)
        {
            _isFullscreen = !_isFullscreen;
            await FullscreenService.SetFullscreenAsync(_isFullscreen);
            _closeOnEscape = !_isFullscreen;
            await InvokeAsync(StateHasChanged);
        }
    }
}
