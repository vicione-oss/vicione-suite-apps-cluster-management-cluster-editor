using System;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices;

public sealed class ToolbarService
{
    public event Action<DataflowToolbarSection>? DataflowToolbarSectionRequested;

    public void RequestDataflowToolbarSection(DataflowToolbarSection section)
        => DataflowToolbarSectionRequested?.Invoke(section);
}
