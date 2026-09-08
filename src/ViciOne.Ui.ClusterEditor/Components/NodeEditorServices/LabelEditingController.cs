using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;

namespace ViciOne.Ui.ClusterEditor.Components.NodeEditorServices;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class LabelEditingController(IDatastore datastore) : IDisposable
{
    private Label? _editingLabel;
    private LabelEditor? _labelEditor;

    public void AttachEditor(LabelEditor editor)
    {
        _labelEditor?.LabelEditorClosed -= OnLabelEditorClosed;

        _labelEditor = editor;
        _labelEditor.LabelEditorClosed += OnLabelEditorClosed;
    }

    public void Dispose()
    {
        _labelEditor?.LabelEditorClosed -= OnLabelEditorClosed;

        _labelEditor = null;
    }

    private void OnLabelEditorClosed(string content)
    {
        if (_editingLabel is null)
            return;

        datastore.Builder.Editors.Label.SetContent(_editingLabel, content);

        _editingLabel = null;
    }

    public Task Show(LabelNode labelNode)
    {
        var model = datastore.DataflowDiagramMapping.GetModel(labelNode);
        _editingLabel = model;

        var content = _editingLabel.Content ?? string.Empty;

        return _labelEditor!.Show(content);
    }
}
