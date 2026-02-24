using System;
using System.Collections.Generic;

namespace ViciOne.Ui.ClusterEditor.Models;

public sealed class EnumWrapper<T> where T : struct, IConvertible
{
    public string DisplayText { get; set; } = string.Empty;
    public EnumWrapper<T> Self => this;
    public T Value { get; set; }

    public EnumWrapper()
    { }

    public EnumWrapper(T enumValue)
    {
        DisplayText = enumValue.ToString()!;
        Value = enumValue;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EnumWrapper<T> wrapper)
            return false;

        return wrapper.DisplayText == DisplayText
            && wrapper.Value.Equals(Value);
    }

    public static IEnumerable<EnumWrapper<T>> FromEnum()
    {
        var result = new List<EnumWrapper<T>>();

        foreach (var value in Enum.GetValues(typeof(T)))
            result.Add(new EnumWrapper<T>((T)value));

        return result;
    }

    public static IEnumerable<EnumWrapper<T>> FromEnumValueList(IEnumerable<T> enumList)
    {
        var result = new List<EnumWrapper<T>>();

        foreach (var value in enumList)
            result.Add(new EnumWrapper<T>(value));

        return result;
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(DisplayText);
        code.Add(Value);
        return code.ToHashCode();
    }
}
