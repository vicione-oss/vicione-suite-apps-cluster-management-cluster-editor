using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

public sealed class IAggregatingPoolingPropertyValueEqualityComparer : IPropertyValueEqualityComparer<IAggregatingPooling>
{
    public bool Equals(IAggregatingPooling? x, IAggregatingPooling? y)
        => string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);

    public int GetHashCode([DisallowNull] IAggregatingPooling obj)
        => HashCode.Combine(obj.Name);
}
