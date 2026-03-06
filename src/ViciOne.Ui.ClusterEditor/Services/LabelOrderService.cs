using System.Collections.Generic;
using System.Linq;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public class LabelOrderService(IDatastore datastore, DiagramService diagramService)
{
    public void BringToFront(IEnumerable<LabelNode> labelNodes)
    {
        var labels = labelNodes.ToArray();
        if (labels.Length == 0)
            return;

        var orderedLabels = datastore
            .DataflowDiagramMapping
            .GetLabelDiagramModels()
            .Except(labels)
            .OrderBy(o => o.Order)
            .Concat(labels.OrderBy(l => l.Order));

        diagramService.Diagram.SuspendSorting = true;

        var order = 1;
        foreach (var label in orderedLabels)
        {
            label.Order = order;
            order++;
        }

        diagramService.Diagram.SuspendSorting = false;
        diagramService.Diagram.RefreshOrders();
    }

    public void SendToBack(IEnumerable<LabelNode> labelNodes)
    {
        var labels = labelNodes.ToArray();
        if (labels.Length == 0)
            return;

        diagramService.Diagram.SuspendSorting = true;

        // OrderBy here to keep the order between the selected labels
        foreach (var label in labels.OrderByDescending(l => l.Order))
        {
            diagramService.Diagram.SendToBack(label);
        }

        diagramService.Diagram.SuspendSorting = false;
        diagramService.Diagram.RefreshOrders();
    }
}
