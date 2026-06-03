using System;
using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Components.UniversalInput.Models;

public sealed class PropertyBag : Dictionary<string, object>
{
    public event Action? CollectionChanged;

    public new object this[string key]
    {
        get => base[key];
        set
        {
            if (base[key] != value)
            {
                base[key] = value;
                InvokeCollectionChanged();
            }
        }
    }

    public new void Add(string key, object value)
    {
        if (ContainsKey(key))
        {
            base[key] = value;
            return;
        }

        base.Add(key, value);
        InvokeCollectionChanged();
    }

    private void InvokeCollectionChanged()
        => CollectionChanged?.Invoke();

    public new bool Remove(string key)
    {
        var result = base.Remove(key);
        if (result)
            InvokeCollectionChanged();
        return result;
    }
}
