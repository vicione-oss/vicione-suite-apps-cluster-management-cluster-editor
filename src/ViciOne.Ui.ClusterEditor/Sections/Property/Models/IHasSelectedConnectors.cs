using System.Collections.Generic;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models;

internal interface IHasSelectedConnectors
{
    IList<IConnector> SelectedConnectors { get; }
}
