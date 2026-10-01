using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Extensions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

public sealed partial class Cell : ComponentBase
{
    private ElementReference _containerElement;
    private bool _editing;
    private object? _editValue;
    private bool _pendingFocus;
    private object? _previousValue;
    private bool _skipRender;
    private string? _validationMessage;

    [Inject] private IDatastore Datastore { get; set; } = default!;
    [Inject] private ILogger<Cell> Logger { get; set; } = default!;

    /// <summary>
    /// Opens the edit and takes the focus each time the parameters are set with it on.
    /// </summary>
    [Parameter]
    public bool Activated { get; set; }

    /// <summary>
    /// The imported <c>Cell.razor.js</c> module. Focus handling is skipped while it is <see langword="null"/>.
    /// </summary>
    [Parameter]
    public IJSObjectReference? JsModule { get; set; }

    /// <summary>Renders the read text bold.</summary>
    [Parameter]
    public bool Modified { get; set; }

    /// <summary>Raised when a valid edit ends or a boolean is toggled.</summary>
    [Parameter]
    public EventCallback<object?> OnValueCommitted { get; set; }

    [Parameter]
    public string? SearchText { get; set; }

    /// <summary>The entered value is validated against this setting.</summary>
    [Parameter, EditorRequired]
    public Setting Setting { get; set; } = default!;

    [Parameter, EditorRequired]
    public object? Value { get; set; }

    [Parameter, EditorRequired]
    public Type ValueType { get; set; } = default!;

    private int ContainerTabIndex
    {
        get
        {
            if (IsBoolean(out _))
                return -1;

            if (_editing)
                return -1;

            return 0;
        }
    }

    private string ReadCssClasses => Modified ? "cell-read modified" : "cell-read";

    private Task CheckedChanged(bool? value)
        => OnValueCommitted.InvokeAsync(value);

    private void EditorValueChanged(object? value)
    {
        try
        {
            Datastore.Builder.Editors.Setting.ValidateValue(Setting, value);

            _editValue = value;
            _validationMessage = null;
        }
        catch (Exception ex)
        {
            _validationMessage = ex.Message;
        }
    }

    private void EnterEditMode()
    {
        if (_editing)
            return;

        _editing = true;
        _editValue = Value;
        _validationMessage = null;
        _pendingFocus = true;
    }

    private async Task FocusOut()
    {
        if (JsModule is null || !_editing)
            return;

        // Under Interactive Server the browser has already completed the focus transition by the time this
        // call reaches it, so document.activeElement can be read straight away.
        var (success, focusInside) = await JsModule.TryInvoke<bool>(Logger, "isFocusInside", _containerElement);
        if (!success || focusInside)
            return;

        _editing = false;

        if (!Equals(_editValue, Value) && _validationMessage is null)
            await OnValueCommitted.InvokeAsync(_editValue);
    }

    private bool IsBoolean(out bool isNullable)
    {
        if (ValueType != typeof(bool) && ValueType != typeof(bool?))
        {
            isNullable = false;
            return false;
        }

        isNullable = ValueType.IsNullableValueType() || Value is null;
        return true;
    }

    private async Task KeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not ("Escape" or "Enter"))
        {
            // Every other key belongs to the editor. Rendering the cell for it renders the editor again as well,
            // and a SpinEdit takes that render for a new value and drops the step its arrow key has just made.
            _skipRender = true;
            return;
        }

        if (JsModule is null)
            return;

        if (e.Key == "Escape" && _editing)
        {
            _editValue = Value;
            _validationMessage = null;

            _editing = false;

            await JsModule.TryInvokeVoid(Logger, "focusTable", _containerElement);
        }
        else if (e.Key == "Enter")
        {
            await JsModule.TryInvokeVoid(Logger, "focusTable", _containerElement);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingFocus && _editing && JsModule is not null)
        {
            _pendingFocus = false;
            await JsModule.TryInvokeVoid(Logger, "focusFirstInput", _containerElement);
        }
    }

    protected override void OnParametersSet()
    {
        if (_editing && Equals(_editValue, _previousValue))
            _editValue = Value;

        _previousValue = Value;

        if (Activated)
            EnterEditMode();
    }

    protected override bool ShouldRender()
    {
        if (!_skipRender)
            return true;

        _skipRender = false;
        return false;
    }
}
