using ViciOne.Cluster.Builder;
using ViciOne.Core.Contracts.DataModel;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Extensions;

internal static class IAggregatingPoolingExtensions
{
    public static AvailableAggregatingPooling ToAvailableAggregatingPooling(this IAggregatingPooling? aggregatingPooling)
        => new(aggregatingPooling?.Name ?? string.Empty, aggregatingPooling?.Description ?? string.Empty);
}
