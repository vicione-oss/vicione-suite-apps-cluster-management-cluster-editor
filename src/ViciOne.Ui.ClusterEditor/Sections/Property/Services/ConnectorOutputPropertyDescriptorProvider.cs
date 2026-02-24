using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

internal sealed class ConnectorOutputPropertyDescriptorProvider<TContext>(Datastore datastore)
    : IPropertyDescriptorProvider<TContext, ConnectorOutput>
{
    public IEnumerable<IPropertyDescriptor<ConnectorOutput>> GetPropertyDescriptors(TContext context)
    {
        var editor = datastore.Builder.Editors.Connector;

        yield return new DescriptionPropertyDescriptor<ConnectorOutput>(editor);
        yield return new EventEnabledPropertyDescriptor<ConnectorOutput>(editor);
        yield return new MarkAsChangedOnlyIfNotEqualPropertyDescriptor<ConnectorOutput>(editor);
        yield return new NamePropertyDescriptor<ConnectorOutput>();
        yield return new PublishedPropertyDescriptor<ConnectorOutput>(editor);
    }
}
