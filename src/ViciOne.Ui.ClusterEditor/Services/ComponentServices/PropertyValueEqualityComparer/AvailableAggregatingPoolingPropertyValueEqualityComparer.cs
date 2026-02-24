using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

public sealed class AvailableAggregatingPoolingPropertyValueEqualityComparer : IPropertyValueEqualityComparer<AvailableAggregatingPooling>
{
    public bool Equals(AvailableAggregatingPooling? x, AvailableAggregatingPooling? y)
        => string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);

    public int GetHashCode([DisallowNull] AvailableAggregatingPooling obj)
        => HashCode.Combine(obj.Name);
}
