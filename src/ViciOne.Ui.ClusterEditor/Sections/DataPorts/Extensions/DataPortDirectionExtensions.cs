using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.NodeTypes;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;

internal static class DataPortDirectionExtensions
{
    public static DataPortTransferDirection? TranslateToTreeBuilderModel(this DataPortDirection? direction)
        => direction switch
        {
            DataPortDirection.In => DataPortTransferDirection.Inbound,
            DataPortDirection.Out => DataPortTransferDirection.Outbound,
            _ => null
        };
}
