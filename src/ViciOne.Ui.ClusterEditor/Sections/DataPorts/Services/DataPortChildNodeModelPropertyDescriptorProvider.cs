using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.UiControlTypes;
using ViciOne.Ui.Blazor.Components.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Validators;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class DataPortChildNodeModelPropertyDescriptorProvider(
    DataPortChildNodePropertyValueStore propertyValueStore, NumericPropertyDescriptorBuilderProvider numericPropertyDescriptorBuilderProvider)
        : IPropertyDescriptorProvider<DataPortChildNodeEditContext, DataPortChildNodeModel>
{
    private readonly Dictionary<string, List<IDataPortNodeModelProperty>> _dependencyMap = [];
    private readonly Dictionary<(Delegate, Type), MethodInfo> _genericMethodCache = [];
    private readonly Dictionary<string, IPropertyDescriptor> _propertyDescriptors = [];
    private readonly StringMustNotBeEmptyPropertyValueValidator _stringMustNotBeEmptyPropertyValueValidator = new();

    private IPropertyDescriptor<DataPortChildNodeModel>? CreateCustomPropertyDescriptor(DataPortNodeModelCustomProperty property)
    {
        if (property.PossibleValues?.Count > 0)
        {
            var createMethod = GetOrCreateGenericMethod(CreateSelectionPropertyDescriptor<object>, property.RuntimeType);
            return createMethod.Invoke(this, [property, property.PossibleValues, GetDependencies(property.Name)])
                as IPropertyDescriptor<DataPortChildNodeModel>;
        }

        if (property.Type.UiControl == NumericUpDownType.TypeKey)
        {
            var builder = numericPropertyDescriptorBuilderProvider.GetBuilder(property.RuntimeType);

            if (property.MinValue is not null)
                builder.WithMinimum(property.MinValue);

            if (property.MaxValue is not null)
                builder.WithMaximum(property.MaxValue);

            return builder.Build<DataPortChildNodeModel>(
                CreateNumericPropertyDescriptor<object, int, int, int>, property, GetDependencies(property.Name));
        }

        var method = GetOrCreateGenericMethod(CreatePropertyDescriptorWithDefaultValue<object>, property.RuntimeType);
        return method.Invoke(this, [property, property.Type.DefaultValue, GetDependencies(property.Name)])
            as IPropertyDescriptor<DataPortChildNodeModel>;
    }

    private void CreateDependencyMap(IReadOnlyCollection<IDataPortNodeModelProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.DependentProperties == null) continue;

            foreach (var dependencyName in prop.DependentProperties.Keys)
            {
                if (!_dependencyMap.TryGetValue(dependencyName, out var list))
                {
                    list = [];
                    _dependencyMap[dependencyName] = list;
                }

                list.Add(prop);
            }
        }
    }

    private NumericPropertyDescriptor<Models.DataPortChildNodeModel, TPropertyValue, TInterval, TLimit> CreateNumericPropertyDescriptor<DataPortChildNodeModel, TPropertyValue, TInterval, TLimit>(
        TInterval interval, TLimit minimum, TLimit maximum, IDataPortNodeModelProperty property, IReadOnlyCollection<IPropertyDescriptor>? dependencies)
            where TInterval : struct
            where TLimit : struct
        => new()
        {
            Category = property.Category,
            DependsOn = dependencies,
            GetValue = (instance) => propertyValueStore.Get<TPropertyValue>(property.Name, defaultValue: default!),
            Interval = interval,
            IsRasteredValue = true,
            Maximum = maximum,
            Minimum = minimum,
            Name = property.Name,
            SetValue = (instance, value) => propertyValueStore.Set(property.Name, value),
            Visible = (instance) => DetermineVisibility(property.Name, instance.Properties)
        };

    private PropertyDescriptor<DataPortChildNodeModel, TPropertyValue> CreatePropertyDescriptor<TPropertyValue>(
        IDataPortNodeModelProperty property, IReadOnlyCollection<IPropertyDescriptor>? dependencies)
        => new()
        {
            CanBeSetToNull = default(TPropertyValue) is null || typeof(TPropertyValue).IsNullableValueType(),
            Category = property.Category,
            DependsOn = dependencies,
            GetValue = (instance) => propertyValueStore.Get<TPropertyValue>(property.Name, default!),
            Name = property.Name,
            SetValue = (instance, value) => propertyValueStore.Set(property.Name, value),
            Visible = (instance) => DetermineVisibility(property.Name, instance.Properties)
        };

    private PropertyDescriptor<DataPortChildNodeModel, TPropertyValue> CreatePropertyDescriptorWithDefaultValue<TPropertyValue>(
        IDataPortNodeModelProperty property, TPropertyValue defaultValue, IReadOnlyCollection<IPropertyDescriptor>? dependencies)
        => new()
        {
            CanBeSetToNull = defaultValue is null || typeof(TPropertyValue).IsNullableValueType(),
            Category = property.Category,
            DependsOn = dependencies,
            GetDefaultValue = (instance) => defaultValue,
            GetValue = (instance) => propertyValueStore.Get(property.Name, defaultValue),
            HasValueDifferentFromDefaultValue = (instance, defaultValue) => !Equals(propertyValueStore.Get(property.Name, defaultValue), defaultValue),
            Name = property.Name,
            SetValue = (instance, value) => propertyValueStore.Set(property.Name, value),
            Visible = (instance) => DetermineVisibility(property.Name, instance.Properties),
        };

    private SelectionPropertyDescriptor<DataPortChildNodeModel, TPropertyValue> CreateSelectionPropertyDescriptor<TPropertyValue>(
        IDataPortNodeModelProperty property, Dictionary<object, string> possibleValues, IReadOnlyCollection<IPropertyDescriptor>? dependencies)
        => new()
        {
            Category = property.Category,
            DependsOn = dependencies,
            GetSelectableValues = (instance) => possibleValues
                .Where(p => p.Key is TPropertyValue)
                .Select(kvp => new SelectableValue<TPropertyValue> { Text = kvp.Value, Value = (TPropertyValue)kvp.Key }),
            GetValue = (instance) => propertyValueStore.Get<TPropertyValue>(property.Name, defaultValue: default!),
            Name = property.Name,
            SetValue = (instance, value) => propertyValueStore.Set(property.Name, value),
            Visible = (instance) => DetermineVisibility(property.Name, instance.Properties),
        };

    private bool DetermineVisibility(string propertyName, IEnumerable<IDataPortNodeModelProperty> allProperties)
    {
        foreach (var property in allProperties)
        {
            if (property.DependentProperties is null || !property.DependentProperties.TryGetValue(propertyName, out var allowedValues))
                continue;

            if (!allowedValues.Contains(propertyValueStore.Get<object>(property.Name, default!)))
                return false;
        }

        return true;
    }

    private SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortDirection> GetDataPortDirectionPropertyData(
        DataPortTreeNodeSystemProperty<DataPortDirection> directionProperty, Guid nodeId, IClusterBuilder clusterBuilder)
    {
        var selectableValues = new Lazy<SelectableValue<DataPortDirection>[]>(() =>
        {
            if (!clusterBuilder.Cache.DataPortIds.TryGetValue(nodeId, out var clusterDataPort))
                return [];

            return [.. directionProperty.AvailableValues
                .Where(t => clusterBuilder.Editors.DataPort.CanSetDirection(clusterDataPort, t))
                .Select(v => new SelectableValue<DataPortDirection> { Text = v.ToString(), Value = v })];
        });

        propertyValueStore.Set(directionProperty.Name, directionProperty.TypedValue);

        return new SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortDirection>
        {
            Category = directionProperty.Category,
            Enabled = (instance) => selectableValues.Value.Length > 1,
            GetSelectableValues = (instance) => selectableValues.Value,
            GetValue = (instance) => propertyValueStore.Get<DataPortDirection>(directionProperty.Name, defaultValue: default),
            Name = directionProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(directionProperty.Name, value)
        };
    }

    private List<IPropertyDescriptor>? GetDependencies(string propertyName)
    {
        if (!_dependencyMap.TryGetValue(propertyName, out var dependencies))
            return null;

        List<IPropertyDescriptor>? result = null;

        foreach (var item in dependencies)
        {
            if (_propertyDescriptors.TryGetValue(item.Name, out var descriptor))
            {
                result ??= [];
                result.Add(descriptor);
            }
        }

        return result;
    }

    private MethodInfo GetOrCreateGenericMethod(Delegate factoryDelegate, Type typeArgument)
    {
        var key = (factoryDelegate, typeArgument);
        if (!_genericMethodCache.TryGetValue(key, out var method))
        {
            method = factoryDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(typeArgument);
            _genericMethodCache[key] = method;
        }

        return method;
    }

    public IEnumerable<IPropertyDescriptor<DataPortChildNodeModel>> GetPropertyDescriptors(DataPortChildNodeEditContext context)
    {
        propertyValueStore.Clear();
        propertyValueStore.Set(nameof(context.Node.Name), context.Node.Name);

        yield return new PropertyDescriptor<DataPortChildNodeModel, string>
        {
            Category = DataPortNodeModelSystemProperty.SystemCategory,
            Enabled = (instance) => !instance.NameIsReadOnly,
            GetValue = (instance) => propertyValueStore.Get(nameof(instance.Name), defaultValue: string.Empty),
            Name = nameof(DataPortChildNodeModel.Name),
            SetValue = (instance, value) => propertyValueStore.Set(nameof(instance.Name), value),
            ValueValidators = [_stringMustNotBeEmptyPropertyValueValidator]
        };

        if (context.Node.AvailableIcons.Count > 1)
        {
            propertyValueStore.Set(nameof(context.Node.Icon), context.Node.Icon);

            yield return new SelectionPropertyDescriptor<DataPortChildNodeModel, string>()
            {
                GetSelectableValues = (instance) => instance.AvailableIcons.Select(icon => new SelectableValue<string> { Text = icon, Value = icon }),
                GetValue = (instance) => propertyValueStore.Get(nameof(instance.Icon), defaultValue: string.Empty),
                Name = nameof(DataPortChildNodeModel.Icon),
                SetValue = (instance, value) => propertyValueStore.Set(nameof(instance.Icon), value)
            };
        }

        var systemProperties = context.Node.Properties.OfType<DataPortNodeModelSystemProperty>().ToList();

        CreateDependencyMap(systemProperties);

        foreach (var property in OrderByDependency(systemProperties))
        {
            var systemProperty = property switch
            {
                DataPortTreeNodeSystemProperty<DataPortDirection> directionProperty
                    => GetDataPortDirectionPropertyData(directionProperty, context.Node.Id.Value, context.ClusterBuilder),

                DataPortTreeNodeSystemProperty<DataPortTransferMode> transferModeProperty
                    => GetTransferModePropertyData(transferModeProperty),

                DataPortTreeNodeSystemProperty<string> stringProperty
                    => GetStringPropertyData(stringProperty, context.Node, context.ClusterBuilder),

                DataPortTreeNodeSystemProperty<uint?> uintProperty
                    => GetUintPropertyData(uintProperty, GetDependencies(property.Name)),

                _ => GetRegularPropertyData((DataPortNodeModelSystemProperty)property)
            };

            _propertyDescriptors.Add(property.Name, systemProperty);

            yield return systemProperty;
        }

        var customProperties = context.Node.Properties.OfType<DataPortNodeModelCustomProperty>().ToList();

        CreateDependencyMap(customProperties);

        foreach (var property in OrderByDependency(customProperties))
        {
            propertyValueStore.Set(property.Name, property.Value);

            var descriptor = CreateCustomPropertyDescriptor((DataPortNodeModelCustomProperty)property);
            if (descriptor is not null)
            {
                _propertyDescriptors.Add(property.Name, descriptor);
                yield return descriptor;
            }
        }

        _propertyDescriptors.Clear();
        _dependencyMap.Clear();
        _genericMethodCache.Clear();
    }

    private IPropertyDescriptor<DataPortChildNodeModel> GetRegularPropertyData(DataPortNodeModelSystemProperty property)
    {
        propertyValueStore.Set(property.Name, property.Value);

        var propertyValueType = property.Value?.GetType() ?? typeof(string);

        var createMethod = GetOrCreateGenericMethod(CreatePropertyDescriptor<object>, propertyValueType);

        var invokeResult = createMethod.Invoke(this, [property, GetDependencies(property.Name)]);
        if (invokeResult is not IPropertyDescriptor<DataPortChildNodeModel> result)
            throw new InvalidOperationException();

        return result;
    }

    private IPropertyDescriptor<DataPortChildNodeModel> GetStringPropertyData(DataPortTreeNodeSystemProperty<string> stringProperty,
        DataPortChildNodeModel node, IClusterBuilder clusterBuilder)
    {
        propertyValueStore.Set(stringProperty.Name, stringProperty.TypedValue);

        if (IsValueTypeProperty(stringProperty))
            return GetValueTypePropertyData(node, stringProperty, clusterBuilder);

        if (stringProperty.AvailableValues.Count > 0)
        {
            var selectableValues = stringProperty.AvailableValues
                .Select(v => new SelectableValue<string> { Text = v, Value = v })
                .ToArray();

            return new SelectionPropertyDescriptor<DataPortChildNodeModel, string>()
            {
                Category = stringProperty.Category,
                Enabled = (instance) => selectableValues.Length > 1,
                GetSelectableValues = (instance) => selectableValues,
                GetValue = (instance) => propertyValueStore.Get(stringProperty.Name, defaultValue: string.Empty),
                Name = stringProperty.Name,
                SetValue = (instance, value) => propertyValueStore.Set(stringProperty.Name, value)
            };
        }

        return new PropertyDescriptor<DataPortChildNodeModel, string>
        {
            Category = stringProperty.Category,
            GetValue = (instance) => propertyValueStore.Get(stringProperty.Name, defaultValue: string.Empty),
            Name = stringProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(stringProperty.Name, value)
        };
    }

    private SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortTransferMode> GetTransferModePropertyData(
        DataPortTreeNodeSystemProperty<DataPortTransferMode> transferModeProperty)
    {
        propertyValueStore.Set(transferModeProperty.Name, transferModeProperty.TypedValue);

        var selectableValues = transferModeProperty.AvailableValues
            .Select(v => new SelectableValue<DataPortTransferMode> { Text = v.ToString(), Value = v })
            .ToArray();

        return new SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortTransferMode>
        {
            Category = transferModeProperty.Category,
            Enabled = (instance) => selectableValues.Length > 1,
            GetSelectableValues = (instance) => selectableValues,
            GetValue = (instance) => propertyValueStore.Get<DataPortTransferMode>(transferModeProperty.Name, defaultValue: default),
            Name = transferModeProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(transferModeProperty.Name, value)
        };
    }

    private PropertyDescriptor<DataPortChildNodeModel, uint?> GetUintPropertyData(DataPortTreeNodeSystemProperty<uint?> uintProperty, IReadOnlyCollection<IPropertyDescriptor>? dependencies)
    {
        propertyValueStore.Set(uintProperty.Name, uintProperty.TypedValue);

        return new NumericPropertyDescriptor<DataPortChildNodeModel, uint?, uint, uint>
        {
            Category = uintProperty.Category,
            DependsOn = dependencies,
            GetValue = (instance) => propertyValueStore.Get<uint?>(uintProperty.Name, defaultValue: default),
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = uint.MinValue,
            Name = uintProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(uintProperty.Name, value),
            Visible = (instance) => DetermineVisibility(uintProperty.Name, instance.Properties)
        };
    }

    private SelectionPropertyDescriptor<DataPortChildNodeModel, string> GetValueTypePropertyData(DataPortChildNodeModel node,
        DataPortTreeNodeSystemProperty<string> stringProperty, IClusterBuilder clusterBuilder)
    {
        var treeBuilder = node.RootNode.Builder;

        SelectableValue<string>[] selectableValues;
        if (!clusterBuilder.Cache.DataPortTreeNodeIds.TryGetValue(node.Id.Value, out var clusterNode))
        {
            selectableValues = [];
        }
        else
        {
            selectableValues = [.. stringProperty.AvailableValues
                .Where(t => clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(clusterNode, treeBuilder.DataTypes[t].RuntimeType))
                .Select(v => new SelectableValue<string> { Text = v, Value = v })];
        }

        return new SelectionPropertyDescriptor<DataPortChildNodeModel, string>
        {
            Category = stringProperty.Category,
            Enabled = (instance) => selectableValues.Length > 1,
            GetSelectableValues = (instance) => selectableValues,
            GetValue = (instance) => propertyValueStore.Get(stringProperty.Name, defaultValue: string.Empty),
            InformationTooltip = selectableValues.Length < 2 ? Components.Localization.DataPortSection.ValueTypeDisabledInformation : null,
            Name = stringProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(stringProperty.Name, value)
        };
    }

    private static bool IsValueTypeProperty(DataPortTreeNodeSystemProperty<string> stringProperty)
        => stringProperty.Name == nameof(DataPortTreeNode.ValueType);

    private List<IDataPortNodeModelProperty> OrderByDependency(IReadOnlyCollection<IDataPortNodeModelProperty> properties)
    {
        var sorted = new List<IDataPortNodeModelProperty>();
        var state = new Dictionary<string, bool>();

        foreach (var property in properties)
            Visit(property);

        return sorted;

        void Visit(IDataPortNodeModelProperty property)
        {
            if (state.TryGetValue(property.Name, out var done))
            {
                if (!done)
                    throw new InvalidOperationException($"Circular dependency detected at '{property.Name}'");

                return;
            }

            state[property.Name] = false;

            if (_dependencyMap.TryGetValue(property.Name, out var dependents))
            {
                foreach (var dependent in dependents)
                    Visit(dependent);
            }

            state[property.Name] = true;
            sorted.Add(property);
        }
    }
}
