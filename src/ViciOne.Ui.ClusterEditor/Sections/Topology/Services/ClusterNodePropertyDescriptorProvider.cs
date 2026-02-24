using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class ClusterNodePropertyDescriptorProvider : IPropertyDescriptorProvider<TopologyNodeEditContext, ClusterNode>
{
    private readonly Lazy<SelectableValue<ClusterNodeType>[]> _typeSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<ClusterNodeType>();

    public IEnumerable<IPropertyDescriptor<ClusterNode>> GetPropertyDescriptors(TopologyNodeEditContext context)
    {
        yield return new PropertyDescriptor<ClusterNode, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(ClusterNode.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<ClusterNode, string?>
        {
            CanBeSetToNull = true,
            GetValue = (instance) => instance.Description,
            Name = nameof(ClusterNode.Description),
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new SelectionPropertyDescriptor<ClusterNode, ClusterNodeType>
        {
            GetSelectableValues = (instance) => _typeSelectableValues.Value,
            GetValue = (instance) => instance.Type,
            Name = nameof(ClusterNode.Type),
            SetValue = (instance, value) => instance.Type = value
        };

        yield return new NumericPropertyDescriptor<ClusterNode, int, int, int>
        {
            GetValue = (instance) => instance.ComputePowerLevel,
            Interval = 1,
            Maximum = int.MaxValue,
            Minimum = 0,
            Name = nameof(ClusterNode.ComputePowerLevel),
            SetValue = (instance, value) => instance.ComputePowerLevel = value
        };
    }
}
