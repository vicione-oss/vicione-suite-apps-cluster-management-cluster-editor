using System;
using System.Collections.Generic;
using ViciOne.Cluster.Builder;

namespace ViciOne.Ui.ClusterEditor.Models.TypeComparer;

public sealed class AvailableAggregatingPoolingComparer : Comparer<AvailableAggregatingPooling>
{
    public override int Compare(AvailableAggregatingPooling? x, AvailableAggregatingPooling? y)
    {
        if (x is null || y is null)
        {
            if (x is null && y is null)
                return 0;

            if (x is null)
                return -1;

            if (y is null)
                return 1;
        }

        return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
    }
}
