using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Models.PropertyDescriptors.ConnectorInputs;

internal sealed class CrossSourcePoolingStrategyPropertyDescriptor<TConnectorInput>
    : SelectionPropertyDescriptor<TConnectorInput, PoolingStrategy>
        where TConnectorInput : class, IConnectorInput
{
    [SetsRequiredMembers]
    public CrossSourcePoolingStrategyPropertyDescriptor(ClusterBuilder clusterBuilder,
        Func<TConnectorInput, ConnectorInput> connectorInputSelector)
    {
        var editor = clusterBuilder.Editors.Connector;

        Category = TechnicalTerms.Pooling;

        GetDefaultValue = (instance) =>
        {
            var containerInput = connectorInputSelector(instance);

            return clusterBuilder.ResolveDefaultCrossSourcePoolingStrategy(containerInput);
        };

        GetSelectableValues = (instance) =>
        {
            var containerInput = connectorInputSelector(instance);

            return editor.GetAvailablePoolingStrategies(containerInput)
                .Select(s => new SelectableValue<PoolingStrategy> { Text = s.ToString(), Value = s });
        };

        GetValue = (instance) =>
        {
            var containerInput = connectorInputSelector(instance);

            return containerInput.CrossSourcePoolingStrategy;
        };

        Name = nameof(IConnectorInput.CrossSourcePoolingStrategy);

        ResetValue = (instance) =>
        {
            var containerInput = connectorInputSelector(instance);

            editor.ResetCrossSourcePoolingStrategy(containerInput, out _);
            editor.ResetPerSourcePoolingStrategy(containerInput, out _);
        };

        SetValue = (instance, value) =>
        {
            var containerInput = connectorInputSelector(instance);

            editor.SetCrossSourcePoolingStrategy(containerInput, value, out _);
            editor.SetPerSourcePoolingStrategy(containerInput, value, out _);
        };
    }
}
