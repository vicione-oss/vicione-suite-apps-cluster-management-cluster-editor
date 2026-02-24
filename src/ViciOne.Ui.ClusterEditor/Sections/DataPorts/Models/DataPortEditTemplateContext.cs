using System;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;

internal sealed class DataPortEditTemplateContext
{
    public event Action<DataPortNodeModel>? Cancel;
    public event Action<DataPortNodeModel>? Confirm;

    public void InvokeCancel(DataPortNodeModel node)
        => Cancel?.Invoke(node);

    public void InvokeConfirm(DataPortNodeModel node)
        => Confirm?.Invoke(node);
}
