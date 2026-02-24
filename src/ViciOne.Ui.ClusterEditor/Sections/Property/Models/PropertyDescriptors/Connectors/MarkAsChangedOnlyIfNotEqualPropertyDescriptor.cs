using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;

internal sealed class MarkAsChangedOnlyIfNotEqualPropertyDescriptor<TConnector> : PropertyDescriptor<TConnector, bool>
    where TConnector : class, IConnector
{
    [SetsRequiredMembers]
    public MarkAsChangedOnlyIfNotEqualPropertyDescriptor(ConnectorEditor editor)
    {
        Category = CommonVocabulary.Data;
        GetDefaultValue = (instance) => ConnectorDefaults.MarkAsChangedOnlyIfNotEqual;
        GetValue = (instance) => instance.MarkAsChangedOnlyIfNotEqual;
        Name = nameof(Connector.MarkAsChangedOnlyIfNotEqual);
        ResetValue = editor.ResetMarkAsChangedOnlyIfNotEqual;
        SetValue = editor.SetMarkAsChangedOnlyIfNotEqual;
    }
}
