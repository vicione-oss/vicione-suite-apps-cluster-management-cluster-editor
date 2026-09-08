using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

namespace ViciOne.Ui.ClusterEditor.Services.ComponentServices.FbSettingsEditor;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class FbSettingsEditorRequest : IFbSettingsEditorRequest
{
    public event Func<Task>? FbSettingsEditorRequested;

    public async Task SendAsync()
    {
        if (FbSettingsEditorRequested is not null)
            await FbSettingsEditorRequested.Invoke();
    }
}
