using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;

internal sealed class EventEnabledPropertyDescriptor<TConnector> : PropertyDescriptor<TConnector, bool>
    where TConnector : class, IConnector
{
    [SetsRequiredMembers]
    public EventEnabledPropertyDescriptor(ConnectorEditor editor)
    {
        Category = CommonVocabulary.Data;
        GetDefaultValue = (instance) => ConnectorDefaults.EventEnabled;
        GetValue = (instance) => instance.EventEnabled;
        Name = nameof(Connector.EventEnabled);
        ResetValue = editor.ResetEventEnabled;
        SetValue = (instance, value) => editor.SetEventEnabled(value, instance);
    }
}
