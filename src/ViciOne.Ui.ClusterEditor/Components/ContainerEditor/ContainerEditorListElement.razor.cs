using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor;

public sealed partial class ContainerEditorListElement : ComponentBase
{
    [Parameter] public required ContainerEditorConnector Connector { get; set; }
    [Parameter] public EventCallback<OnContainerEditorListElementClickedArgs> OnClick { get; set; }

    private void InvokeOnClick(bool ctrlKey, bool shiftKey)
        => OnClick.InvokeAsync(new(Connector, ctrlKey, shiftKey));
}
