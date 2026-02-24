using System;
using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

internal sealed class ClusterApplicationPropertyDescriptorProvider : IPropertyDescriptorProvider<TopologyNodeEditContext, ClusterApplication>
{
    private readonly Lazy<SelectableValue<ClusterApplicationType>[]> _typeSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<ClusterApplicationType>();

    public IEnumerable<IPropertyDescriptor<ClusterApplication>> GetPropertyDescriptors(TopologyNodeEditContext context)
    {
        yield return new PropertyDescriptor<ClusterApplication, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(ClusterApplication.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<ClusterApplication, string?>
        {
            CanBeSetToNull = true,
            GetValue = (instance) => instance.Description,
            Name = nameof(ClusterApplication.Description),
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new SelectionPropertyDescriptor<ClusterApplication, ClusterApplicationType>
        {
            GetSelectableValues = (instance) => _typeSelectableValues.Value,
            GetValue = (instance) => instance.Type,
            Name = nameof(ClusterApplication.Type)
        };
    }
}
