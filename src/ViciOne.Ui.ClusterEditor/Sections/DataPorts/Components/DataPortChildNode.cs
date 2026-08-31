using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ViciOne.Ui.ClusterEditor.Components.TreeNodes;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Components;

public sealed class DataPortChildNode : CustomTooltipTreeNode
{
    [Inject] private IDatastore Datastore { get; set; } = default!;

    protected override Task<TooltipInfo> GetTooltipInfo(MouseEventArgs e, Rectangle parentBounds)
        => Task.FromResult(TooltipDataPortData.GetDataPortTooltipInfo(Datastore, e, (DataPortNodeModel)Node.TreeNode, parentBounds));
}
