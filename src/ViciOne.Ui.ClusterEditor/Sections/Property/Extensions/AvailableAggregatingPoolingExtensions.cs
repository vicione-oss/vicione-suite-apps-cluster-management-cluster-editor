using ViciOne.Cluster.Builder;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Extensions;

internal static class AvailableAggregatingPoolingExtensions
{
    public static AvailableAggregatingPoolingName GetStronglyTypedName(this AvailableAggregatingPooling availableAggregatingPooling)
        => new(availableAggregatingPooling.Name);
}
