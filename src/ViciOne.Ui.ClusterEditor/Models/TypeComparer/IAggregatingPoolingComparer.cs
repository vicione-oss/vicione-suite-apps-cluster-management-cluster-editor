using System;
using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Ui.ClusterEditor.Models.TypeComparer;

public sealed class IAggregatingPoolingComparer : Comparer<IAggregatingPooling>
{
    public override int Compare(IAggregatingPooling? x, IAggregatingPooling? y)
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
