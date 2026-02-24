using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarDataflow.Models;

internal sealed class DataflowToolbarPropertyGridContext : IHasSelectedConnectors
{
    public IList<IConnector> SelectedConnectors { get; init; } = [];
}
