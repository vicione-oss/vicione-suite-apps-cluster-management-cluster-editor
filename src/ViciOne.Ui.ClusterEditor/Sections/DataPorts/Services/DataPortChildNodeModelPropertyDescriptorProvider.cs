using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<string, List<DataPortNodeModelCustomProperty>> _dependencyMap = [];
    private readonly Dictionary<string, IPropertyDescriptor> _propertyDescriptors = [];
    private readonly StringMustNotBeEmptyPropertyValueValidator _stringMustNotBeEmptyPropertyValueValidator = new();

    private void CreateDependencyMap(IReadOnlyCollection<DataPortNodeModelCustomProperty> properties)
    {
        foreach (var prop in properties)
        {
            if (prop.DependentProperties == null) continue;

            foreach (var dependencyName in prop.DependentProperties.Keys)
            {
                if (!_dependencyMap.TryGetValue(dependencyName, out var list))
                {
                    list ??= [];
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
            Visible = (instance) => DetermineVisibility<TPropertyValue>(property.Name, instance.Properties.Where(p => p.DependentProperties?.ContainsKey(property.Name) ?? false))
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
            Visible = (instance) => DetermineVisibility<TPropertyValue>(property.Name, instance.Properties.Where(p => p.DependentProperties?.ContainsKey(property.Name) ?? false))
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
            Visible = (instance) => DetermineVisibility<TPropertyValue>(property.Name, instance.Properties.Where(p => p.DependentProperties?.ContainsKey(property.Name) ?? false)),
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
            Visible = (instance) => DetermineVisibility<TPropertyValue>(property.Name, instance.Properties.Where(p => p.DependentProperties?.ContainsKey(property.Name) ?? false)),
        };

    private bool DetermineVisibility<TPropertyValue>(string propertyName, IEnumerable<IDataPortNodeModelProperty> dataPortNodeModelProperties)
    {
        var result = true;

        foreach (var property in dataPortNodeModelProperties)
        {
            result = property.DependentProperties?.FirstOrDefault(d => d.Key == propertyName).Value.Contains(propertyValueStore.Get<object>(property.Name, default!)) ?? false;

            if (!result)
                return result;
        }

        return result;
    }

    private SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortDirection> GetDataPortDirectionPropertyData(
        DataPortTreeNodeSystemProperty<DataPortDirection> directionProperty, Guid nodeId, IClusterBuilder clusterBuilder)
    {
        var selectableValues = new Lazy<SelectableValue<DataPortDirection>[]>(() =>
        {
            var clusterDataPort = clusterBuilder.Cache.DataPorts.FirstOrDefault(k => k.Id == nodeId);

            if (clusterDataPort is null)
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
        var dependencies = _dependencyMap.GetValueOrDefault(propertyName) ?? [];
        var result = new List<IPropertyDescriptor>();

        foreach (var item in dependencies)
        {
            if (_propertyDescriptors.TryGetValue(item.Name, out var descriptor))
                result.Add(descriptor);
        }

        return result.Count > 0 ? result : null;
    }

    public IEnumerable<IPropertyDescriptor<DataPortChildNodeModel>> GetPropertyDescriptors(DataPortChildNodeEditContext context)
    {
        propertyValueStore.Clear();
        propertyValueStore.Set(nameof(context.Node.DisplayText), context.Node.DisplayText);

        yield return new PropertyDescriptor<DataPortChildNodeModel, string>
        {
            Category = DataPortNodeModelSystemProperty.SystemCategory,
            Enabled = (instance) => !instance.DisplayTextIsReadOnly,
            GetValue = (instance) => propertyValueStore.Get(nameof(instance.DisplayText), defaultValue: string.Empty),
            Name = nameof(DataPortChildNodeModel.DisplayText),
            SetValue = (instance, value) => propertyValueStore.Set(nameof(instance.DisplayText), value),
            ValueValidators = [_stringMustNotBeEmptyPropertyValueValidator]
        };

        if (context.Node.AvailableIcons.Count() > 1)
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

        foreach (var property in context.Node.Properties.OfType<DataPortNodeModelSystemProperty>())
        {
            yield return property switch
            {
                DataPortTreeNodeSystemProperty<DataPortDirection> directionProperty
                    => GetDataPortDirectionPropertyData(directionProperty, context.Node.Id.Value, context.ClusterBuilder),

                DataPortTreeNodeSystemProperty<DataPortTransferMode> transferModeProperty
                    => GetTransferModePropertyData(transferModeProperty),

                DataPortTreeNodeSystemProperty<string> stringProperty
                    => GetStringPropertyData(stringProperty, context.Node, context.ClusterBuilder),

                DataPortTreeNodeSystemProperty<uint?> uintProperty
                    => GetUintPropertyData(uintProperty),

                _ => GetRegularPropertyData(property)
            };
        }

        var customeProperties = context.Node.Properties.OfType<DataPortNodeModelCustomProperty>().ToList();

        CreateDependencyMap(customeProperties);

        foreach (var property in OrderByDependency(customeProperties))
        {
            propertyValueStore.Set(property.Name, property.Value);

            if (property.PossibleValues?.Count > 0)
            {
                var propertyValueType = property.RuntimeType;

                var createDelegate = CreateSelectionPropertyDescriptor<object>;
                var createMethod = createDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(propertyValueType);

                var invokeResult = createMethod.Invoke(this, [property, property.PossibleValues, GetDependencies(property.Name)]);
                if (invokeResult is IPropertyDescriptor<DataPortChildNodeModel> result)
                {
                    _propertyDescriptors.Add(property.Name, result);

                    yield return result;
                }
            }
            else if (property.Type.UiControl == NumericUpDownType.TypeKey)
            {
                var builder = numericPropertyDescriptorBuilderProvider.GetBuilder(property.RuntimeType);

                if (property.MinValue is not null)
                    builder.WithMinimum(property.MinValue);

                if (property.MaxValue is not null)
                    builder.WithMaximum(property.MaxValue);

                var propertyDescriptor = builder.Build<DataPortChildNodeModel>(
                    CreateNumericPropertyDescriptor<object, int, int, int>, property, GetDependencies(property.Name));

                if (propertyDescriptor is not null)
                {
                    _propertyDescriptors.Add(property.Name, propertyDescriptor);

                    yield return propertyDescriptor;
                }
            }
            else
            {
                var propertyValueType = property.RuntimeType;

                var createDelegate = CreatePropertyDescriptorWithDefaultValue<object>;
                var createMethod = createDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(propertyValueType);

                var invokeResult = createMethod.Invoke(this, [property, property.Type.DefaultValue, GetDependencies(property.Name)]);
                if (invokeResult is IPropertyDescriptor<DataPortChildNodeModel> propertyDescriptor)
                {
                    _propertyDescriptors.Add(property.Name, propertyDescriptor);

                    yield return propertyDescriptor;
                }
            }
        }

        _propertyDescriptors.Clear();
        _dependencyMap.Clear();
    }

    private IPropertyDescriptor<DataPortChildNodeModel> GetRegularPropertyData(DataPortNodeModelSystemProperty property)
    {
        propertyValueStore.Set(property.Name, property.Value);

        var propertyValueType = property.Value?.GetType() ?? typeof(string);

        var createDelegate = CreatePropertyDescriptor<object>;
        var createMethod = createDelegate.Method.GetGenericMethodDefinition().MakeGenericMethod(propertyValueType);

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

        return stringProperty.AvailableValues.Count > 0
            ? new SelectionPropertyDescriptor<DataPortChildNodeModel, string>()
            {
                Category = stringProperty.Category,
                Enabled = (instance) => stringProperty.AvailableValues.Count > 1,
                GetSelectableValues = (instance) => stringProperty.AvailableValues.Select(v => new SelectableValue<string> { Text = v, Value = v }),
                GetValue = (instance) => propertyValueStore.Get(stringProperty.Name, defaultValue: string.Empty),
                Name = stringProperty.Name,
                SetValue = (instance, value) => propertyValueStore.Set(stringProperty.Name, value)
            }
            : new PropertyDescriptor<DataPortChildNodeModel, string>
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

        return new SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortTransferMode>
        {
            Category = transferModeProperty.Category,
            Enabled = (instance) => transferModeProperty.AvailableValues.Count > 1,
            GetSelectableValues = (instance) => transferModeProperty.AvailableValues.Select(v => new SelectableValue<DataPortTransferMode> { Text = v.ToString(), Value = v }),
            GetValue = (instance) => propertyValueStore.Get<DataPortTransferMode>(transferModeProperty.Name, defaultValue: default),
            Name = transferModeProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(transferModeProperty.Name, value)
        };
    }

    private PropertyDescriptor<DataPortChildNodeModel, uint?> GetUintPropertyData(DataPortTreeNodeSystemProperty<uint?> uintProperty)
    {
        propertyValueStore.Set(uintProperty.Name, uintProperty.TypedValue);

        return new NumericPropertyDescriptor<DataPortChildNodeModel, uint?, uint, uint>
        {
            Category = uintProperty.Category,
            GetValue = (instance) => propertyValueStore.Get<uint?>(uintProperty.Name, defaultValue: default),
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = uint.MinValue,
            Name = uintProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(uintProperty.Name, value)
        };
    }

    private SelectionPropertyDescriptor<DataPortChildNodeModel, string> GetValueTypePropertyData(DataPortChildNodeModel node,
        DataPortTreeNodeSystemProperty<string> stringProperty, IClusterBuilder clusterBuilder)
    {
        var treeBuilder = node.RootNode.Builder;
        var selectableValues = new Lazy<SelectableValue<string>[]>(() =>
        {
            var clusterNode = clusterBuilder.Cache.DataPortTreeNodes.FirstOrDefault(k => k.Id == node.Id.Value);

            if (clusterNode is null)
                return [];

            return [.. stringProperty.AvailableValues
                .Where(t => clusterBuilder.Editors.DataPortTreeNode.CanSetValueType(clusterNode, treeBuilder.DataTypes[t].RuntimeType))
                .Select(v => new SelectableValue<string> { Text = v, Value = v })];
        });

        return new SelectionPropertyDescriptor<DataPortChildNodeModel, string>
        {
            Category = stringProperty.Category,
            Enabled = (instance) => selectableValues.Value.Length > 1,
            GetSelectableValues = (instance) => selectableValues.Value,
            GetValue = (instance) => propertyValueStore.Get(stringProperty.Name, defaultValue: string.Empty),
            InformationTooltip = selectableValues.Value.Length < 2 ? Components.Localization.DataPortSection.ValueTypeDisabledInformation : null,
            Name = stringProperty.Name,
            SetValue = (instance, value) => propertyValueStore.Set(stringProperty.Name, value)
        };
    }

    private static bool IsValueTypeProperty(DataPortTreeNodeSystemProperty<string> stringProperty)
        => stringProperty.Name == nameof(DataPortTreeNode.ValueType);

    private List<DataPortNodeModelCustomProperty> OrderByDependency(IReadOnlyCollection<DataPortNodeModelCustomProperty> properties)
    {
        var sorted = new List<DataPortNodeModelCustomProperty>();
        var visiting = new HashSet<string>();
        var propertyByName = properties.ToDictionary(p => p.Name);

        foreach (var property in properties)
            Visit(property);

        return sorted;

        void Visit(DataPortNodeModelCustomProperty property)
        {
            if (sorted.Contains(property))
                return;

            if (visiting.Contains(property.Name))
                throw new InvalidOperationException($"Circular dependency detected at '{property.Name}'");

            visiting.Add(property.Name);

            if (_dependencyMap.TryGetValue(property.Name, out var dependents))
            {
                foreach (var dependent in dependents)
                    Visit(dependent);
            }

            visiting.Remove(property.Name);
            sorted.Add(property);
        }
    }
}
