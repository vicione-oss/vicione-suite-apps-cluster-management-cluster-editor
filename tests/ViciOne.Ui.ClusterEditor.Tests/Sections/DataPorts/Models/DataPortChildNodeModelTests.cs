using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
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
        var builder = new TreeBuilder.TreeBuilder(TestResources.MqttRuleset);
        var rootNode = new DataPortRootNodeModel
        {
            Builder = builder,
            Name = builder.Ruleset.Root?.Name ?? "MQTT DataPort",
        };

        return new DataPortChildNodeModel()
        {
            Name = "Test",
            Parent = rootNode,
            Properties = properties ?? [],
            RootNode = rootNode,
            TransferDirections = [],
        };
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
        var expectedPropertyDescriptorCount = childNode.AvailableIcons.Any() ? 5 : 4;

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
