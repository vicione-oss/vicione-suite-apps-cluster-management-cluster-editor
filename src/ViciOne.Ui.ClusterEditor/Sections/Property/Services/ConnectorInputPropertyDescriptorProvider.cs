using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Property.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ConnectorInputs;
using ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.Connectors;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

internal sealed class ConnectorInputPropertyDescriptorProvider<TContext>(Datastore datastore,
    ConnectorInputPropertyDescriptorFactory propertyDescriptorFactory)
        : IPropertyDescriptorProvider<TContext, ConnectorInput>
            where TContext : class, IHasSelectedConnectors
{
    public IEnumerable<IPropertyDescriptor<ConnectorInput>> GetPropertyDescriptors(TContext context)
    {
        var builder = datastore.Builder;

        var crossSourcePoolingStrategyPropertyDescriptor = new CrossSourcePoolingStrategyPropertyDescriptor<ConnectorInput>(
            builder, connectorInputSelector: connectorInput => connectorInput);

        yield return crossSourcePoolingStrategyPropertyDescriptor;

        yield return new CrossSourceAggregatingPoolingPropertyDescriptor<ConnectorInput>(builder,
            crossSourcePoolingStrategyPropertyDescriptor, connectorInputSelector: connectorInput => connectorInput);

        var valuePropertyDescriptors = propertyDescriptorFactory.CreateValuePropertyDescriptors<ConnectorInput>(
            context.SelectedConnectors, connectorSelector: connectorInput => connectorInput);

        foreach (var valuePropertyDescriptor in valuePropertyDescriptors)
            yield return valuePropertyDescriptor;

        var editor = builder.Editors.Connector;

        yield return new DescriptionPropertyDescriptor<ConnectorInput>(editor);
        yield return new EventEnabledPropertyDescriptor<ConnectorInput>(editor);
        yield return new MarkAsChangedOnlyIfNotEqualPropertyDescriptor<ConnectorInput>(editor);
        yield return new NamePropertyDescriptor<ConnectorInput>();
        yield return new PublishedPropertyDescriptor<ConnectorInput>(editor);
    }
}
