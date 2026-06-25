using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.TreeEditor.Builder.Interface.NodeIdentifier;

namespace ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

public sealed class TopologyTreeViewModel(object dataItem) : IClusterEditorTreeNode
{
    public List<TopologyTreeViewModel> Children { get; init; } = [];
    public object DataItem { get; } = dataItem;
    public bool Expanded { get; set; }
    public bool HasChangedProperties { get; set; }
    public bool Highlighted { get; set; }
    public INodeIdentifier Id
    {
        get => new GuidNodeIdentifier(GetUnderlyingId(DataItem));
        set { }
    }
    public bool IsConfigurable { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsEditable { get; set; }
    public bool IsEditModeActive { get; set; }
    public bool IsManuallySelected { get; set; }
    public string Name { get; set; } = string.Empty;
    public TopologyTreeViewModel? Parent { get; set; }
    public bool Selected { get; set; }

    private static Guid GetUnderlyingId(object dataItem) => dataItem switch
    {
        Cluster.Model.Cluster cluster => cluster.Id,
        ClusterNodeGroup nodeGroup => nodeGroup.Id,
        ClusterNode node => node.Id,
        ClusterApplication application => application.Id,
        EngineHost engineHost => engineHost.Id,
        Cluster.Model.Engine engine => engine.Id,
        DataPort dataPort => dataPort.Id,
        _ => Guid.Empty
    };
}
