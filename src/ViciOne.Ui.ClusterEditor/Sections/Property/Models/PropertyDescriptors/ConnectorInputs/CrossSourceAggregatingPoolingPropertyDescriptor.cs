using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;
using ViciOne.Ui.ClusterEditor.Sections.Property.Extensions;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ConnectorInputs;

internal sealed class CrossSourceAggregatingPoolingPropertyDescriptor<TConnectorInput>
    : SelectionPropertyDescriptor<TConnectorInput, AvailableAggregatingPoolingName>
        where TConnectorInput : class, IConnectorInput
{
    [SetsRequiredMembers]
    public CrossSourceAggregatingPoolingPropertyDescriptor(IClusterBuilder clusterBuilder,
        CrossSourcePoolingStrategyPropertyDescriptor<TConnectorInput> crossSourcePoolingStrategyPropertyDescriptor,
        Func<TConnectorInput, ConnectorInput> connectorInputSelector)
    {
        var editor = clusterBuilder.Editors.Connector;

        Category = TechnicalTerms.Pooling;
        DependsOn = [crossSourcePoolingStrategyPropertyDescriptor];

        GetDefaultValue = (instance) => clusterBuilder.ResolveDefaultCrossSourceAggregatingPooling(connectorInputSelector(instance))
            .ToAvailableAggregatingPooling()
            .GetStronglyTypedName();

        GetSelectableValues = (instance) => editor.GetAvailableAggregatingPoolings(connectorInputSelector(instance))
            .Select(p => new SelectableValue<AvailableAggregatingPoolingName> { Text = p.Name, Value = p.GetStronglyTypedName() });

        GetValue = (instance) => instance.CrossSourceAggregatingPooling.ToAvailableAggregatingPooling().GetStronglyTypedName();

        Name = nameof(IConnectorInput.CrossSourceAggregatingPooling);

        Resettable = (instance) => clusterBuilder.ResolveDefaultCrossSourceAggregatingPooling(connectorInputSelector(instance)) is not null;
        ResetValue = (instance) =>
        {
            editor.ResetCrossSourceAggregatingPooling(instance);
            editor.ResetPerSourceAggregatingPooling(instance);
        };

        SetValue = (instance, value) =>
        {
            var connectorInput = connectorInputSelector(instance);

            var aggregatingPooling = editor.GetAvailableAggregatingPoolings(connectorInput)
                .First(p => p.Name == value.Value);

            editor.CreateAndSetCrossSourceAggregatingPooling(aggregatingPooling, connectorInput);
            editor.CreateAndSetPerSourceAggregatingPooling(aggregatingPooling, connectorInput);
        };

        Visible = (instance) => instance.CrossSourcePoolingStrategy == PoolingStrategy.Aggregate;
    }
}
