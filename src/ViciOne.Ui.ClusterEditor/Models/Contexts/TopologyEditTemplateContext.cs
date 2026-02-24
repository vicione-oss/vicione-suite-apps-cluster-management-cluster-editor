using System;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Models.Contexts;

internal sealed class TopologyEditTemplateContext
{
    public event Action<TopologyTreeViewModel>? Cancel;
    public event Action<TopologyTreeViewModel, object?>? Confirm;

    public void InvokeCancel(TopologyTreeViewModel node)
        => Cancel?.Invoke(node);

    public void InvokeConfirm(TopologyTreeViewModel node, object? editItem)
        => Confirm?.Invoke(node, editItem);
}
