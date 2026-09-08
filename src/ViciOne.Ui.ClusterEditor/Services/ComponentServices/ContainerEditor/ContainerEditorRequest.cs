using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.ContainerEditor;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ContainerEditorRequest : IContainerEditorRequest
{
    public event Func<Task>? ContainerEditorRequestedAsync;

    public async Task SendAsync()
    {
        if (ContainerEditorRequestedAsync is not null)
            await ContainerEditorRequestedAsync.Invoke();
    }
}
