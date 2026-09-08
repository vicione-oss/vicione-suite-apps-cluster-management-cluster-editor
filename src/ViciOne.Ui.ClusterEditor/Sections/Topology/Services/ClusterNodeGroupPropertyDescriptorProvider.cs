using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Factories;
using ViciOne.Ui.ClusterEditor.Sections.Topology.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ClusterNodeGroupPropertyDescriptorProvider : IPropertyDescriptorProvider<TopologyNodeEditContext, ClusterNodeGroup>
{
    private readonly Lazy<SelectableValue<NetworkType>[]> _networkTypeSelectableValues =
        SelectableValueCollectionFactory.CreateLazy<NetworkType>();

    public IEnumerable<IPropertyDescriptor<ClusterNodeGroup>> GetPropertyDescriptors(TopologyNodeEditContext context)
    {
        yield return new PropertyDescriptor<ClusterNodeGroup, string>
        {
            GetValue = (instance) => instance.Name,
            Name = nameof(ClusterNodeGroup.Name),
            SetValue = (instance, value) => instance.Name = value
        };

        yield return new PropertyDescriptor<ClusterNodeGroup, string?>
        {
            CanBeSetToNull = true,
            GetValue = (instance) => instance.Description,
            Name = nameof(ClusterNodeGroup.Description),
            SetValue = (instance, value) => instance.Description = value
        };

        yield return new SelectionPropertyDescriptor<ClusterNodeGroup, NetworkType>
        {
            GetSelectableValues = (instance) => _networkTypeSelectableValues.Value,
            GetValue = (instance) => instance.NetworkType,
            Name = nameof(ClusterNodeGroup.NetworkType),
            SetValue = (instance, value) => instance.NetworkType = value
        };
    }
}
