using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;

internal sealed class PublishedPropertyDescriptor<TConnector> : PropertyDescriptor<TConnector, bool>
    where TConnector : class, IConnector
{
    [SetsRequiredMembers]
    public PublishedPropertyDescriptor(ConnectorEditor editor)
    {
        Category = CommonVocabulary.Data;
        GetDefaultValue = (instance) => ConnectorDefaults.Published;
        GetValue = (instance) => instance.Published;
        Name = nameof(ConnectorInput.Published);
        ResetValue = editor.ResetPublished;
        SetValue = editor.SetPublished;
    }
}
