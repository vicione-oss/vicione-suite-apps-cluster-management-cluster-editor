using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class DataPortChildNodePropertyValueStore
{
    private readonly Dictionary<string, object?> _propertyValues = [];

    public IReadOnlyDictionary<string, object?> Values => _propertyValues;

    public void Clear()
        => _propertyValues.Clear();

    public T Get<T>(string propertyName, T defaultValue)
    {
        if (TryGet<T>(propertyName, out var propertyValue))
            return propertyValue;

        return defaultValue;
    }

    public void Set(string propertyName, object? newValue)
        => _propertyValues[propertyName] = newValue;

    public void SetIfAbsent(string propertyName, object? value)
        => _propertyValues.TryAdd(propertyName, value);

    public bool TryGet<T>(string propertyName, [MaybeNullWhen(false)] out T propertyValue)
    {
        if (_propertyValues.TryGetValue(propertyName, out var boxedPropertyValue))
        {
            if (boxedPropertyValue is T unboxedPropertyValue)
            {
                propertyValue = unboxedPropertyValue;
                return true;
            }
        }

        propertyValue = default;

        return false;
    }
}
