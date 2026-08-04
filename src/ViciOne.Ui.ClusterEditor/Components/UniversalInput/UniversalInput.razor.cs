using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;
using ViciOne.Ui.ClusterEditor.Components.UniversalInput.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Components.UniversalInput;

public sealed partial class UniversalInput : ComponentBase, IDisposable
{
    private const string TextBoxPlaceholderEmpty = "<empty>";
    private const string TextBoxPlaceholderNull = "<null>";

    private static readonly Type s_charType = typeof(char);
    private static readonly Type s_dateTimeType = typeof(DateTime);
    private static readonly Type s_timeSpanType = typeof(TimeSpan);
    private static readonly Type s_uriType = typeof(Uri);

    private EditorType _editorType;
    private object? _enumComboBoxItems;
    private PropertyBag? _propertyBag;
    private string _textBoxPlaceholder = TextBoxPlaceholderNull;
    private string? _textBoxValue;
    private EventCallback<string?>? _textBoxValueChangedCallback;
    private Expression<Func<string?>>? _textBoxValueExpression;

    [Inject] private BoxedNumericValueDescriptorBuilderProvider BoxedNumericValueDescriptorBuilderProvider { get; set; } = default!;

    [Parameter] public string CssClass { get; set; } = string.Empty;
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public PropertyBag? PropertyBag { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    [Parameter] public EventCallback<string> UriParseError { get; set; }
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }
    [Parameter, EditorRequired] public Type ValueType { get; set; }

    private EventCallback<string?> TextBoxValueChangedCallback => _textBoxValueChangedCallback
        ??= EventCallback.Factory.Create(this, (string? newValue) => OnTextBoxValueChangedAsync(newValue));
    private Expression<Func<string?>> TextBoxValueExpression => _textBoxValueExpression
        ??= () => _textBoxValue;

    private object? CreateAssignmentCallback(Type valueType)
        => typeof(UniversalInput).GetMethod(nameof(CreateAssignmentEventCallback), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(valueType)
            .Invoke(this, null);

    private EventCallback<T> CreateAssignmentEventCallback<T>()
        => EventCallback.Factory.Create(this, (T newValue) => OnValueChangedAsync(newValue));

    public void Dispose()
        => _propertyBag?.CollectionChanged -= OnPropertyBagCollectionChangedAsync;

    private static object? GetComboBoxItems(Type valueType)
    {
        var @delegate = GetComboBoxItems<DayOfWeek>;

        var method = @delegate.Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(valueType);

        var result = method.Invoke(null, []);

        return result;
    }

    public static IEnumerable<ComboBoxItem<TValue, string>> GetComboBoxItems<TValue>()
        where TValue : struct, Enum
            => Enum.GetValues<TValue>()
                .Select(enumValue => new ComboBoxItem<TValue, string>()
                {
                    Text = enumValue.ToString() ?? string.Empty,
                    Value = enumValue,
                });

    private Type GetDxComboBoxType()
        => typeof(DxComboBox<,>).MakeGenericType(
            [typeof(ComboBoxItem<,>).MakeGenericType(ValueType, typeof(string)), ValueType]
        );

    private EditorType GetEditorType()
    {
        if (ValueType == typeof(bool))
        {
            return EditorType.Boolean;
        }
        else if (ValueType == s_charType)
        {
            return EditorType.Char;
        }
        else if (ValueType == s_dateTimeType)
        {
            return EditorType.DateTime;
        }
        else if (ValueType == s_timeSpanType)
        {
            return EditorType.TimeSpan;
        }
        else if (ValueType.IsEnum)
        {
            return EditorType.Enum;
        }
        else if (ValueType.IsNumeric())
        {
            return EditorType.Numeric;
        }
        else if (ValueType == s_uriType)
        {
            return EditorType.Uri;
        }
        else
        {
            return EditorType.Text;
        }
    }

    private object GetValueExpression(object? value, Type valueType)
        => typeof(UniversalInput).GetMethod(nameof(GetValueExpression), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(valueType)
            .Invoke(this, [value])!;

    private static Expression<Func<T>> GetValueExpression<T>(object? value)
    {
        var val = value is not null ? (T)value : default!;

        return () => val;
    }

    protected override void OnParametersSet()
    {
        _editorType = GetEditorType();

        if (PropertyBag != _propertyBag)
        {
            _propertyBag?.CollectionChanged -= OnPropertyBagCollectionChangedAsync;

            _propertyBag = PropertyBag;

            _propertyBag?.CollectionChanged += OnPropertyBagCollectionChangedAsync;
        }

        _enumComboBoxItems = _editorType is EditorType.Enum ? GetComboBoxItems(ValueType) : null;

        if (_editorType is
                EditorType.Char or
                EditorType.DateTime or
                EditorType.Text or
                EditorType.TimeSpan or
                EditorType.Uri)
        {
            _textBoxValue = Value?.ToString();

            _textBoxPlaceholder = _textBoxValue is null ? TextBoxPlaceholderNull : TextBoxPlaceholderEmpty;
        }
    }

    private async void OnPropertyBagCollectionChangedAsync()
        => await InvokeAsync(StateHasChanged);

    private async Task OnTextBoxValueChangedAsync(string? newValue)
    {
        _textBoxValue = newValue;

        if (_editorType is EditorType.Uri)
        {
            try
            {
                var newValueString = (Convert.ToString(newValue, CultureInfo.InvariantCulture) ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(newValueString))
                {
                    var oldValueString = Convert.ToString(Value, CultureInfo.InvariantCulture);
                    if (oldValueString is not null && !oldValueString.Equals(newValueString, StringComparison.OrdinalIgnoreCase))
                        await ValueChanged.InvokeAsync(newValueString);
                    return;
                }

                var uri = new UriBuilder(newValueString).Uri;
                await ValueChanged.InvokeAsync(uri);
            }
            catch (UriFormatException ufe)
            {
                await UriParseError.InvokeAsync(ufe.Message);
            }
        }
        else if (ValueType == typeof(char))
        {
            if (string.IsNullOrEmpty(newValue))
            {
                await ValueChanged.InvokeAsync(default(char));
            }
            else
            {
                await ValueChanged.InvokeAsync(newValue[0]);
            }
        }
        else if (ValueType == typeof(DateTime))
        {
            if (DateTime.TryParse(newValue, out var dateTime))
            {
                await ValueChanged.InvokeAsync(dateTime);
            }
            else
            {
                await ValueChanged.InvokeAsync(newValue);
            }
        }
        else if (ValueType == typeof(TimeSpan))
        {
            if (TimeSpan.TryParse(newValue, out var timeSpan))
            {
                await ValueChanged.InvokeAsync(timeSpan);
            }
            else
            {
                await ValueChanged.InvokeAsync(newValue);
            }
        }
        else
        {
            await ValueChanged.InvokeAsync(newValue);
        }
    }

    private async Task OnValueChangedAsync<T>(T newValue)
        => await ValueChanged.InvokeAsync(newValue);
}
