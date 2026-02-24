using System;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.FbSettingsEditor;

internal sealed class FbSettingsEditorRequest : IFbSettingsEditorRequest
{
    public event Func<Task>? FbSettingsEditorRequested;

    public async Task SendAsync()
    {
        if (FbSettingsEditorRequested is not null)
            await FbSettingsEditorRequested.Invoke();
    }
}
