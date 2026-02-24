using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Models.Comparer;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

internal sealed class AlphaNumericPropertyValueEqualityComparer : IPropertyValueEqualityComparer<string>
{
    public bool Equals(string? x, string? y)
    {
        if (x is null && y is null)
            return true;

        if (x is null || y is null)
            return false;

        return AlphaNumericComparer<string>.Default.Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] string obj)
        => obj.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
