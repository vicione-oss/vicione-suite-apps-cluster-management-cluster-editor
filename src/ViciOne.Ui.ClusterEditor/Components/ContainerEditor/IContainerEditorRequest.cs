using System;
using System.Threading.Tasks;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

internal interface IContainerEditorRequest
{
    event Func<Task>? ContainerEditorRequestedAsync;

    Task SendAsync();
}
