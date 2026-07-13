using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Contracts;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services.TypeDescriptors;
using ViciOne.Ui.ClusterEditor.Localization.Resources;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using PropertyGridConstants = ViciOne.Ui.Blazor.Components.PropertyGrid.Constants;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Factories;

internal sealed class ConnectorInputPropertyDescriptorFactory(IDatastore datastore,
    NumericPropertyDescriptorBuilderProvider numericPropertyDescriptorBuilderProvider,
    IEnumerable<INumericValueTypeDescriptor> numericValueTypeDescriptors)
{
    private static NumericPropertyDescriptor<TConnector, TValue, TInterval, TLimit>?
        CreateNumericPropertyDescritor<TConnector, TValue, TInterval, TLimit>(TInterval interval, TLimit minimum, TLimit maximum,
            IClusterBuilder clusterBuilder, Func<TConnector, ConnectorInput> connectorSelector, bool canBeSetToNull, TValue fallbackValue)
                where TConnector : class, IConnectorInput
                where TInterval : struct
                where TLimit : struct
                    => new()
                    {
                        CanBeSetToNull = canBeSetToNull,
                        Category = CommonVocabulary.Data,
                        GetDefaultValue = (instance) => clusterBuilder.ResolveDefaultValue(connectorSelector(instance)) is TValue defaultValue
                            ? defaultValue : fallbackValue,
                        GetValue = (instance) => connectorSelector(instance).Value is TValue value ? value : fallbackValue,
                        Interval = interval,
                        Maximum = maximum,
                        Minimum = minimum,
                        Name = nameof(ConnectorInput.Value),
                        ResetValue = clusterBuilder.Editors.Connector.ResetValue,
                        SetValue = (instance, value) => clusterBuilder.Editors.Connector.SetValue(connectorSelector(instance), value)
                    };

    private static PropertyDescriptor<TConnector, TValue>? CreatePropertyDescriptor<TConnector, TValue>(
        IClusterBuilder clusterBuilder, Func<TConnector, ConnectorInput> connectorSelector, bool canBeSetToNull, TValue fallbackValue)
            where TConnector : class, IConnectorInput
                => new()
                {
                    CanBeSetToNull = canBeSetToNull,
                    Category = CommonVocabulary.Data,
                    GetDefaultValue = (instance) => clusterBuilder.ResolveDefaultValue(connectorSelector(instance)) is TValue defaultValue
                        ? defaultValue : fallbackValue,
                    GetValue = (instance) => connectorSelector(instance).Value is TValue value ? value : fallbackValue,
                    Name = nameof(ConnectorInput.Value),
                    ResetValue = clusterBuilder.Editors.Connector.ResetValue,
                    SetValue = (instance, value) => clusterBuilder.Editors.Connector.SetValue(connectorSelector(instance), value)
                };

    public IEnumerable<IPropertyDescriptor<TConnector>> CreateValuePropertyDescriptors<TConnector>(
        IList<IConnector> selectedConnectors, Func<TConnector, ConnectorInput> connectorSelector)
            where TConnector : class, IConnectorInput
    {
        var connectorDesignMap = selectedConnectors
            .OfType<TConnector>()
            .Select(connectorSelector)
            .ToDictionary(keySelector: connector => connector, elementSelector: datastore.Builder.ResolveConnectorDesign);

        var valueTypeConnectorDesignLookup = connectorDesignMap
            .ToLookup(keySelector: p => DataTypeCompatibilityValidator.DetermineValueType(p.Value.ConnectorType),
                elementSelector: p => p.Value);

        if (valueTypeConnectorDesignLookup.Count > 1)
        {
            // Multiple instances having different value properties, do an early return as
            // creating multiple property descriptors for a single property makes no sense.
            //
            // As an example, consider two selected connectors, one having int value type and
            // the other string value type. If we would create two property descriptors for the
            // Value property, then we would have the following problems:
            //
            //  a) PropertyGrid will use each property descriptors on each of the two selected connectors,
            //     so the internal logic of PropertGrid would execute read / write operations for a single connector twice.
            //
            //  b) PropertyGrid will try to find common properties among all property descriptors
            //     whereas the two different property descriptors returned would cancel each other out
            //     because of the different value type, so no property entry would be shown for
            //     the Value property.
            yield break;
        }

        var valueTypeConnectorDesignGrouping = valueTypeConnectorDesignLookup.First();
        var valueType = valueTypeConnectorDesignGrouping.Key;
        var connectorDesigns = valueTypeConnectorDesignGrouping.ToList();

        var canBeSetToNull = connectorDesigns.All(d => d.DefaultValue is null);
        var fallbackValue = canBeSetToNull ? null : connectorDesigns.First(d => d.DefaultValue is not null).DefaultValue;

        if (numericValueTypeDescriptors.Any(d => d.UnderlyingType == valueType))
        {
            // Numeric value type, build numeric property descriptor
            var builder = numericPropertyDescriptorBuilderProvider.GetBuilder(valueType);

            var propertyDescriptor = builder.Build<TConnector>(CreateNumericPropertyDescritor<TConnector, int, int, int>,
                datastore.Builder, connectorSelector, canBeSetToNull, fallbackValue);

            if (propertyDescriptor is not null)
                yield return propertyDescriptor;
        }
        else if (valueType == typeof(string) || PropertyGridConstants.SupportedBooleanPropertyValueTypes.Contains(valueType))
        {
            var createDelegate = CreatePropertyDescriptor<TConnector, object>;
            var createMethod = createDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(typeof(TConnector), valueType);

            var invokeResult = createMethod.Invoke(this,
                [datastore.Builder, connectorSelector, canBeSetToNull, fallbackValue]);

            if (invokeResult is IPropertyDescriptor<TConnector> propertyDescriptor)
                yield return propertyDescriptor;
        }
    }
}
