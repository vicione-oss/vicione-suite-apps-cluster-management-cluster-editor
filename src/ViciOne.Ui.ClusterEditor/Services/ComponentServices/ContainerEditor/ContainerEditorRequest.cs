using System;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.ContainerEditor;

internal sealed class ContainerEditorRequest : IContainerEditorRequest
{
    public event Func<Task>? ContainerEditorRequestedAsync;

    public async Task SendAsync()
    {
        if (ContainerEditorRequestedAsync is not null)
            await ContainerEditorRequestedAsync.Invoke();
    }
}
