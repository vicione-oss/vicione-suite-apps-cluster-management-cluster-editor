using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;

internal sealed class DescriptionPropertyDescriptor<TConnector> : PropertyDescriptor<TConnector, string?>
    where TConnector : class, IConnector
{
    [SetsRequiredMembers]
    public DescriptionPropertyDescriptor(ConnectorEditor editor)
    {
        CanBeSetToNull = true;
        Category = CommonVocabulary.Appearance;
        GetDefaultValue = (instance) => null;
        GetValue = (instance) => instance.Description;
        Name = nameof(Connector.Description);
        ResetValue = editor.ResetDescription;
        SetValue = editor.SetDescription;
    }
}
