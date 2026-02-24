using System;
using System.Threading.Tasks;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;

internal interface IFbSettingsEditorRequest
{
    event Func<Task>? FbSettingsEditorRequested;

    Task SendAsync();
}
