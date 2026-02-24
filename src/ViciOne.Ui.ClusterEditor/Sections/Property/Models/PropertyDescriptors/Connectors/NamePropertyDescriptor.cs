using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;

internal sealed class NamePropertyDescriptor<TConnector> : PropertyDescriptor<TConnector, string>
    where TConnector : IConnector
{
    [SetsRequiredMembers]
    public NamePropertyDescriptor()
    {
        Category = CommonVocabulary.Data;
        Enabled = (instance) => false;
        GetValue = (instance) => instance.Name;
        Name = nameof(Connector.Name);
    }
}
