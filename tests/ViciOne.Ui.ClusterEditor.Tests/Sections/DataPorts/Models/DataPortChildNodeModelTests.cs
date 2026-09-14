using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Tree.Builder.PropertyTypes;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Models;

public class DataPortChildNodeModelTests
{
    private const string StringProperty = nameof(StringProperty);
    private const string UIntProperty = nameof(UIntProperty);

    private static DataPortChildNodeModel CreateChildNode(List<IDataPortNodeModelProperty>? properties)
    {
        var builder = new Tree.Builder.TreeBuilder(TestResources.MqttRuleset);
        var rootNode = new DataPortRootNodeModel
        {
            Builder = builder,
            Name = builder.Ruleset.Root?.Name ?? "MQTT DataPort",
        };

        return new DataPortChildNodeModel()
        {
            LinkDirections = [],
            Name = "Test",
            Parent = rootNode,
            Properties = properties ?? [],
            RootNode = rootNode,
            TransferDirections = [],
        };
    }

    [Fact]
    public void Custom_property_visibility_should_resolve_dependencies_by_ruleset_id()
    {
        // Arrange - the display Name differs from the ruleset Id to prove the dependency
        // is resolved via the ruleset Id and not the Name.
        var urlProperty = new DataPortNodeModelCustomProperty
        {
            Category = "Connection",
            Id = Guid.NewGuid(),
            Name = "Broker URL",
            Reference = new PropertyReference { Id = "Url" },
            RuntimeType = typeof(string),
            Type = new PropertyType { DataType = "string", Id = "Url", Name = "Broker URL" },
        };

        var protocolProperty = new DataPortNodeModelCustomProperty
        {
            Category = "Connection",
            DependentProperties = new() { ["Url"] = [1] },
            Id = Guid.NewGuid(),
            Name = "Protocol",
            Reference = new PropertyReference { Id = "Protocol" },
            RuntimeType = typeof(int),
            Type = new PropertyType { Id = "Protocol", Name = "Protocol" },
            Value = 1,
        };

        var childNode = CreateChildNode([urlProperty, protocolProperty]);

        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        var services = new ServiceCollection()
            .AddScoped<NumericPropertyDescriptorBuilderProvider>();

        services.AddPropertyGrid<DataPortChildNodeEditContext>()
            .WithPropertyDescriptorProvider<DataPortChildNodeModelPropertyDescriptorProvider>();

        services.AddScoped<DataPortChildNodePropertyValueStore>();

        using var serviceProvider = services.BuildServiceProvider();

        var propertyDescriptorProvider = serviceProvider
            .GetRequiredService<IEnumerable<IPropertyDescriptorProvider<DataPortChildNodeEditContext>>>()
            .OfType<DataPortChildNodeModelPropertyDescriptorProvider>()
            .First();

        var propertyValueStore = serviceProvider.GetRequiredService<DataPortChildNodePropertyValueStore>();

        var editContext = new DataPortChildNodeEditContext { ClusterBuilder = clusterBuilder, Node = childNode };

        // Act
        var propertyDescriptors = propertyDescriptorProvider.GetPropertyDescriptors(editContext).ToArray();
        var urlDescriptor = propertyDescriptors.First(descriptor => descriptor.Name == urlProperty.Name);

        // Assert
        propertyValueStore.Set(protocolProperty.Name, 0);
        Assert.False(urlDescriptor.Visible!(childNode));

        propertyValueStore.Set(protocolProperty.Name, 1);
        Assert.True(urlDescriptor.Visible!(childNode));
    }

    [Fact]
    public void Property_data_should_contain_possible_values_if_available()
    {
        // Arrange
        var properties = new List<IDataPortNodeModelProperty>()
        {
            new DataPortTreeNodeSystemProperty<DataPortTransferMode>()
            {
                AvailableValues = [DataPortTransferMode.Periodic, DataPortTransferMode.OnChange],
                DefaultValue = DataPortTransferMode.Periodic,
                Name = nameof(DataPortTransferMode),
                Value = DataPortTransferMode.Periodic,
            },
            new DataPortTreeNodeSystemProperty<uint>()
            {
                Name = UIntProperty,
                TypedDefaultValue = 0,
                TypedValue = 5066
            },
            new DataPortTreeNodeSystemProperty<string>()
            {
                AvailableValues = ["Test", "Best"],
                DefaultValue = "Test",
                Name = StringProperty,
                Value = "Best",
            },
        };

        var childNode = CreateChildNode(properties);
        const int expectedPropertyDescriptorCount = 4;

        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        var services = new ServiceCollection()
            .AddScoped<NumericPropertyDescriptorBuilderProvider>();

        services.AddPropertyGrid<DataPortChildNodeEditContext>()
            .WithPropertyDescriptorProvider<DataPortChildNodeModelPropertyDescriptorProvider>();

        services.AddScoped<DataPortChildNodePropertyValueStore>();

        using var serviceProvider = services.BuildServiceProvider();

        var propertyDescriptorProvider = serviceProvider
            .GetRequiredService<IEnumerable<IPropertyDescriptorProvider<DataPortChildNodeEditContext>>>()
            .OfType<DataPortChildNodeModelPropertyDescriptorProvider>()
            .First();

        var editContext = new DataPortChildNodeEditContext { ClusterBuilder = clusterBuilder, Node = childNode };

        // Act
        var propertyDescriptors = propertyDescriptorProvider.GetPropertyDescriptors(editContext).ToArray();

        // Assert
        Assert.Equal(expectedPropertyDescriptorCount, propertyDescriptors.Length);

        Assert.Equal(2, propertyDescriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortTransferMode>>()
            .First(k => k.Name == nameof(DataPortTransferMode))
            .GetSelectableValues(childNode)
            .Count());

        Assert.IsNotType<ISelectionPropertyDescriptor>(propertyDescriptors.First(k => k.Name == UIntProperty), exactMatch: false);
        Assert.Equal(2, propertyDescriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(k => k.Name == StringProperty)
            .GetSelectableValues(childNode)
            .Count());
    }

    [Fact]
    public void Typed_value_should_match_base_value()
    {
        // Arrange
        var properties = new List<IDataPortNodeModelProperty>()
        {
            new DataPortTreeNodeSystemProperty<DataPortTransferMode>()
            {
                AvailableValues = [.. Enum.GetValues<DataPortTransferMode>()],
                Name = nameof(DataPortTransferMode),
                Value = DataPortTransferMode.Periodic,
            }
        };

        // Act
        var childNode = CreateChildNode(properties);
        var typedProp = childNode.Properties.OfType<DataPortTreeNodeSystemProperty<DataPortTransferMode>>().First();
        var baseProp = childNode.Properties.OfType<DataPortNodeModelSystemProperty>().First();

        // Assert
        Assert.Equal(typedProp.Value, baseProp.Value);
    }
}
