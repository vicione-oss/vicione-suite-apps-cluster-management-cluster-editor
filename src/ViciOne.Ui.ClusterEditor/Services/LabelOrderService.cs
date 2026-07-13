using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;

namespace ViciOne.Ui.ClusterEditor.Services;

public class LabelOrderService(IDatastore datastore, DiagramService diagramService)
{
    public void BringToFront(IEnumerable<LabelNode> labelNodes)
    {
        var labels = new List<LabelNode>(labelNodes);
        if (labels.Count == 0)
            return;

        var labelsSet = new HashSet<LabelNode>(labels);

        var allLabels = datastore
            .DataflowDiagramMapping
            .GetLabelDiagramModels();

        // Build two sorted lists: others first, then selected
        var others = new List<LabelNode>();
        var selected = new List<LabelNode>();

        foreach (var label in allLabels)
        {
            if (labelsSet.Contains(label))
                selected.Add(label);
            else
                others.Add(label);
        }

        others.Sort((a, b) => a.Order.CompareTo(b.Order));
        selected.Sort((a, b) => a.Order.CompareTo(b.Order));

        diagramService.Diagram.SuspendSorting = true;

        var order = 1;
        foreach (var label in others)
        {
            label.Order = order;
            order++;
        }

        foreach (var label in selected)
        {
            label.Order = order;
            order++;
        }

        diagramService.Diagram.SuspendSorting = false;
        diagramService.Diagram.RefreshOrders();
    }

    public void SendToBack(IEnumerable<LabelNode> labelNodes)
    {
        var labels = new List<LabelNode>(labelNodes);
        if (labels.Count == 0)
            return;

        diagramService.Diagram.SuspendSorting = true;

        // Sort descending by order to preserve relative ordering
        labels.Sort((a, b) => b.Order.CompareTo(a.Order));

        foreach (var label in labels)
        {
            diagramService.Diagram.SendToBack(label);
        }

        diagramService.Diagram.SuspendSorting = false;
        diagramService.Diagram.RefreshOrders();
    }
}
