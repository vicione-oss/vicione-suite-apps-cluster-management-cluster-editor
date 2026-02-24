using System;

namespace ViciOne.Ui.ClusterEditor.Models;

[Flags]
public enum SelectionMode
{
    Container = 1,
    FunctionBlock = 2,
    FunctionBlockConnector = 4,
    FunctionBlockLink = 8,
    Label = 16,
    ConnectorMarker = 32,
}
