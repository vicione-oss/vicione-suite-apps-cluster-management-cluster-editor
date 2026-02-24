using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;
using Engine_ = ViciOne.Cluster.Model.Engine;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class EnginePropertyDescriptorProvider : IPropertyDescriptorProvider<TopologyNodeEditContext, Engine_>
{
    private readonly Lazy<SelectableValue<EngineType>[]> _engineTypeSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<EngineType>();

    private readonly Lazy<SelectableValue<LogLevel>[]> _logLevelSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<LogLevel>();

    public IEnumerable<IPropertyDescriptor<Engine_>> GetPropertyDescriptors(TopologyNodeEditContext context)
    {
        yield return new PropertyDescriptor<Engine_, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(Engine_.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<Engine_, string?>
        {
            CanBeSetToNull = true,
            GetValue = (instance) => instance.Description,
            Name = nameof(Engine_.Description),
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new SelectionPropertyDescriptor<Engine_, EngineType>
        {
            GetSelectableValues = (instance) => _engineTypeSelectableValues.Value,
            GetValue = (instance) => instance.EngineType,
            Name = nameof(Engine_.EngineType),
            SetValue = (instance, value) => instance.EngineType = value
        };

        yield return new PropertyDescriptor<Engine_, bool>
        {
            GetValue = (instance) => instance.Enabled,
            Name = nameof(Engine_.Enabled),
            SetValue = (instance, value) => instance.Enabled = value
        };

        yield return new NumericPropertyDescriptor<Engine_, uint, uint, uint>
        {
            GetValue = (instance) => instance.RunIndex,
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = 0,
            Name = nameof(Engine_.RunIndex),
            SetValue = (instance, value) => instance.RunIndex = value
        };

        yield return new NumericPropertyDescriptor<Engine_, uint, uint, uint>
        {
            GetValue = (instance) => instance.MinCycleTime,
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = 0,
            Name = nameof(Engine_.MinCycleTime),
            SetValue = (instance, value) => instance.MinCycleTime = value
        };

        yield return new NumericPropertyDescriptor<Engine_, uint, uint, uint>
        {
            GetValue = (instance) => instance.MaxCycleTime,
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = 0,
            Name = nameof(Engine_.MaxCycleTime),
            SetValue = (instance, value) => instance.MaxCycleTime = value
        };

        yield return new SelectionPropertyDescriptor<Engine_, LogLevel>
        {
            GetSelectableValues = (instance) => _logLevelSelectableValues.Value,
            GetValue = (instance) => instance.LogLevel,
            Name = nameof(Engine_.LogLevel),
            SetValue = (instance, value) => instance.LogLevel = value
        };
    }
}
