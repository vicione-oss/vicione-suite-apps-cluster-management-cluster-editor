using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class EngineHostPropertyDescriptorProvider : IPropertyDescriptorProvider<TopologyNodeEditContext, EngineHost>
{
    private readonly Lazy<SelectableValue<LogLevel>[]> _logLevelSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<LogLevel>();

    public IEnumerable<IPropertyDescriptor<EngineHost>> GetPropertyDescriptors(TopologyNodeEditContext context)
    {
        yield return new PropertyDescriptor<EngineHost, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(EngineHost.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<EngineHost, string?>
        {
            CanBeSetToNull = true,
            GetValue = (instance) => instance.Description,
            Name = nameof(EngineHost.Description),
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new NumericPropertyDescriptor<EngineHost, int, int, int>
        {
            GetValue = (instance) => instance.Replicas,
            Interval = 1,
            Maximum = int.MaxValue,
            Minimum = 0,
            Name = nameof(EngineHost.Replicas),
            SetValue = (instance, value) => instance.Replicas = value

        };

        yield return new SelectionPropertyDescriptor<EngineHost, LogLevel>
        {
            GetSelectableValues = (instance) => _logLevelSelectableValues.Value,
            GetValue = (instance) => instance.LogLevel,
            Name = nameof(EngineHost.LogLevel),
            SetValue = (instance, value) => instance.LogLevel = value
        };
    }
}
