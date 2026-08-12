using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.TreeBuilder.PropertyTypes;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Tests.Resources;
using Xunit;
using DataPortTransferMode = ViciOne.Cluster.Model.DataPortTransferMode;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortChildNodeModelPropertyDescriptorProviderTests
{
    private const string DirectionProperty = nameof(DataPort.Direction);
    private const string TransferModeProperty = nameof(DataPortTreeNode.TransferMode);
    private const string ValueTypeProperty = nameof(DataPortTreeNode.ValueType);

    private static DataPortChildNodeModel CreateChildNode(
        List<IDataPortNodeModelProperty>? properties = null,
        IReadOnlyList<string>? availableIcons = null)
    {
        var builder = new TreeBuilder.TreeBuilder(TestResources.MqttRuleset);
        var rootNode = new DataPortRootNodeModel
        {
            Builder = builder,
            Name = builder.Ruleset.Root?.Name ?? "MQTT DataPort",
        };

        return new DataPortChildNodeModel
        {
            AvailableIcons = availableIcons ?? [],
            Name = "Test",
            Parent = rootNode,
            Properties = properties ?? [],
            RootNode = rootNode,
            TransferDirections = [],
        };
    }

    private static IPropertyDescriptor<DataPortChildNodeModel>[] GetDescriptors(DataPortChildNodeModel node, IClusterBuilder clusterBuilder)
    {
        var services = new ServiceCollection()
            .AddScoped<NumericPropertyDescriptorBuilderProvider>();

        services.AddPropertyGrid<DataPortChildNodeEditContext>()
            .WithPropertyDescriptorProvider<DataPortChildNodeModelPropertyDescriptorProvider>();

        services.AddScoped<DataPortChildNodePropertyValueStore>();

        using var serviceProvider = services.BuildServiceProvider();

        var provider = serviceProvider
            .GetRequiredService<IEnumerable<IPropertyDescriptorProvider<DataPortChildNodeEditContext>>>()
            .OfType<DataPortChildNodeModelPropertyDescriptorProvider>()
            .First();

        var editContext = new DataPortChildNodeEditContext { ClusterBuilder = clusterBuilder, Node = node };

        return [.. provider.GetPropertyDescriptors(editContext)];
    }

    [Fact]
    public void GetPropertyDescriptors_AlwaysYieldsNameDescriptorFirst()
    {
        // Arrange
        var node = CreateChildNode();
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var nameDescriptor = descriptors[0];
        Assert.Equal(nameof(DataPortChildNodeModel.Name), nameDescriptor.Name);
        Assert.Equal(DataPortNodeModelSystemProperty.SystemCategory, nameDescriptor.Category);
    }

    [Fact]
    public void GetPropertyDescriptors_WhenNameIsReadOnly_DisablesNameDescriptor()
    {
        // Arrange
        var node = CreateChildNode();
        node.NameIsReadOnly = true;
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var nameDescriptor = GetDescriptors(node, clusterBuilder)[0];

        // Assert
        Assert.False(nameDescriptor.Enabled!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_NameDescriptor_ExposesTrimmingValueAccessors()
    {
        // Arrange
        var node = CreateChildNode();
        node.Name = "Original";
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var nameDescriptor = (PropertyDescriptor<DataPortChildNodeModel, string>)GetDescriptors(node, clusterBuilder)[0];

        // Assert
        Assert.Equal("Original", nameDescriptor.GetValue!(node));
        nameDescriptor.SetValue!(node, "  Trimmed  ");
        Assert.Equal("Trimmed", nameDescriptor.GetValue!(node));
        Assert.True(nameDescriptor.Enabled!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WhenMultipleAvailableIcons_YieldsIconSelectionDescriptor()
    {
        // Arrange
        var node = CreateChildNode(availableIcons: ["icon-a", "icon-b"]);
        node.Icon = "icon-a";
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var iconDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(descriptor => descriptor.Name == nameof(DataPortChildNodeModel.Icon));
        Assert.Equal(2, iconDescriptor.GetSelectableValues(node).Count());
    }

    [Fact]
    public void GetPropertyDescriptors_WhenSingleAvailableIcon_DoesNotYieldIconDescriptor()
    {
        // Arrange
        var node = CreateChildNode(availableIcons: ["icon-a"]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        Assert.DoesNotContain(descriptors, descriptor => descriptor.Name == nameof(DataPortChildNodeModel.Icon));
    }

    [Fact]
    public void GetPropertyDescriptors_WithTransferModeProperty_YieldsSelectionDescriptor()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<DataPortTransferMode>
            {
                AvailableValues = [DataPortTransferMode.Periodic, DataPortTransferMode.OnChange],
                DefaultValue = DataPortTransferMode.Periodic,
                Name = TransferModeProperty,
                Value = DataPortTransferMode.Periodic,
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var transferModeDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortTransferMode>>()
            .First(descriptor => descriptor.Name == TransferModeProperty);
        Assert.Equal(2, transferModeDescriptor.GetSelectableValues(node).Count());
        Assert.True(transferModeDescriptor.Enabled!(node));
        Assert.Equal(DataPortTransferMode.Periodic, transferModeDescriptor.GetDefaultValue!(node));
        Assert.Equal(DataPortTransferMode.Periodic, transferModeDescriptor.GetValue!(node));
        transferModeDescriptor.SetValue!(node, DataPortTransferMode.OnChange);
        Assert.Equal(DataPortTransferMode.OnChange, transferModeDescriptor.GetValue!(node));
        transferModeDescriptor.ResetValue!(node);
        Assert.Equal(DataPortTransferMode.Periodic, transferModeDescriptor.GetValue!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithDirectionProperty_WhenNodeCached_YieldsSettableDirections()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<DataPortDirection>
            {
                AvailableValues = [DataPortDirection.In, DataPortDirection.Out],
                Name = DirectionProperty,
                TypedDefaultValue = DataPortDirection.In,
                TypedValue = DataPortDirection.In,
            },
        ]);

        var clusterDataPort = new DataPort { Id = node.Id.Value };
        var clusterBuilder = Substitute.For<IClusterBuilder>();
        clusterBuilder.Cache.DataPortIds.TryGetValue(node.Id.Value, out Arg.Any<DataPort?>()!)
            .Returns(call =>
            {
                call[1] = clusterDataPort;
                return true;
            });

        // Allow only "In" to be set as the direction.
        clusterBuilder.Editors.DataPort
            .CanSetDirection(clusterDataPort, Arg.Any<DataPortDirection>())
            .Returns(call => (DataPortDirection)call[1] == DataPortDirection.In);

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var directionDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortDirection>>()
            .First(descriptor => descriptor.Name == DirectionProperty);
        var selectable = directionDescriptor.GetSelectableValues(node).ToArray();
        Assert.Single(selectable);
        Assert.Equal(DataPortDirection.In, selectable[0].Value);
    }

    [Fact]
    public void GetPropertyDescriptors_WithDirectionProperty_WhenNodeNotCached_YieldsSelectionWithNoSelectableValues()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<DataPortDirection>
            {
                AvailableValues = [DataPortDirection.In, DataPortDirection.Out],
                Name = DirectionProperty,
                TypedDefaultValue = DataPortDirection.In,
                TypedValue = DataPortDirection.In,
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var directionDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, DataPortDirection>>()
            .First(descriptor => descriptor.Name == DirectionProperty);
        Assert.Empty(directionDescriptor.GetSelectableValues(node));
        Assert.False(directionDescriptor.Enabled!(node));
        Assert.Equal(DataPortDirection.In, directionDescriptor.GetDefaultValue!(node));
        Assert.Equal(DataPortDirection.In, directionDescriptor.GetValue!(node));
        directionDescriptor.SetValue!(node, DataPortDirection.Out);
        Assert.Equal(DataPortDirection.Out, directionDescriptor.GetValue!(node));
        directionDescriptor.ResetValue!(node);
        Assert.Equal(DataPortDirection.In, directionDescriptor.GetValue!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithValueTypeProperty_WhenNodeNotCached_YieldsSelectionWithTooltip()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<string>
            {
                AvailableValues = ["Int32", "String"],
                Name = ValueTypeProperty,
                TypedDefaultValue = "Int32",
                TypedValue = "Int32",
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var valueTypeDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(descriptor => descriptor.Name == ValueTypeProperty);
        Assert.Empty(valueTypeDescriptor.GetSelectableValues(node));
        Assert.NotNull(valueTypeDescriptor.InformationTooltip);
    }

    [Fact]
    public void GetPropertyDescriptors_WithValueTypeProperty_WhenNodeCached_YieldsSettableValueTypes()
    {
        // Arrange
        var availableValues = new[] { "Int32", "String" };
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<string>
            {
                AvailableValues = [.. availableValues],
                Name = ValueTypeProperty,
                TypedDefaultValue = "Int32",
                TypedValue = "Int32",
            },
        ]);

        var treeBuilder = node.RootNode.Builder;
        var clusterNode = new DataPortTreeNode { Id = node.Id.Value };

        var clusterBuilder = Substitute.For<IClusterBuilder>();
        clusterBuilder.Cache.DataPortTreeNodeIds.TryGetValue(node.Id.Value, out Arg.Any<DataPortTreeNode?>()!)
            .Returns(call =>
            {
                call[1] = clusterNode;
                return true;
            });

        // Allow only "Int32" to be set as the value type.
        var int32RuntimeType = treeBuilder.DataTypes["Int32"].RuntimeType;
        clusterBuilder.Editors.DataPortTreeNode
            .CanSetValueType(clusterNode, Arg.Any<Type>())
            .Returns(call => (Type)call[1] == int32RuntimeType);

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var valueTypeDescriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(descriptor => descriptor.Name == ValueTypeProperty);
        var selectable = valueTypeDescriptor.GetSelectableValues(node).ToArray();
        Assert.Single(selectable);
        Assert.Equal("Int32", selectable[0].Value);
        Assert.False(valueTypeDescriptor.Enabled!(node));
        Assert.Equal("Int32", valueTypeDescriptor.GetDefaultValue!(node));
        Assert.Equal("Int32", valueTypeDescriptor.GetValue!(node));
        valueTypeDescriptor.SetValue!(node, "String");
        Assert.Equal("String", valueTypeDescriptor.GetValue!(node));
        valueTypeDescriptor.ResetValue!(node);
        Assert.Equal("Int32", valueTypeDescriptor.GetValue!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithStringPropertyWithAvailableValues_YieldsSelectionDescriptor()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<string>
            {
                AvailableValues = ["First", "Second"],
                Name = "StringChoice",
                TypedDefaultValue = "First",
                TypedValue = "Second",
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var descriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(d => d.Name == "StringChoice");
        Assert.Equal(2, descriptor.GetSelectableValues(node).Count());
        Assert.Equal("First", descriptor.GetDefaultValue!(node));
        Assert.Equal("Second", descriptor.GetValue!(node));
        descriptor.SetValue!(node, "First");
        Assert.Equal("First", descriptor.GetValue!(node));
        descriptor.ResetValue!(node);
        Assert.True(descriptor.Enabled!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithStringPropertyWithoutAvailableValues_YieldsPlainPropertyDescriptor()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<string>
            {
                Name = "FreeText",
                TypedDefaultValue = "default",
                TypedValue = "value",
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var descriptor = descriptors.First(d => d.Name == "FreeText");
        var typed = Assert.IsType<PropertyDescriptor<DataPortChildNodeModel, string>>(descriptor);
        Assert.Equal("value", typed.GetValue!(node));
        Assert.Equal("default", typed.GetDefaultValue!(node));
        typed.SetValue!(node, "changed");
        Assert.Equal("changed", typed.GetValue!(node));
        typed.ResetValue!(node);
        Assert.Equal("default", typed.GetValue!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithNullableUIntProperty_YieldsNumericDescriptor()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<uint?>
            {
                Name = "Port",
                TypedDefaultValue = 0,
                TypedValue = 5066,
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var descriptor = (NumericPropertyDescriptor<DataPortChildNodeModel, uint?, uint, uint>)descriptors.First(d => d.Name == "Port");
        Assert.Equal(5066u, descriptor.GetValue!(node));
        Assert.Equal(0u, descriptor.GetDefaultValue!(node));
        descriptor.SetValue!(node, 42u);
        Assert.Equal(42u, descriptor.GetValue!(node));
        descriptor.ResetValue!(node);
        Assert.Equal(0u, descriptor.GetValue!(node));
        Assert.True(descriptor.Visible!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithCustomPropertyWithPossibleValues_YieldsSelectionDescriptor()
    {
        // Arrange
        var customProperty = new DataPortNodeModelCustomProperty
        {
            Category = "Custom",
            DefaultValue = 1,
            Id = Guid.NewGuid(),
            Name = "Mode",
            PossibleValues = new Dictionary<object, string> { [1] = "One", [2] = "Two" },
            Reference = new PropertyReference { Id = "Mode" },
            RuntimeType = typeof(int),
            Type = new PropertyType { Id = "Mode", Name = "Mode" },
            Value = 1,
        };
        var node = CreateChildNode([customProperty]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptors = GetDescriptors(node, clusterBuilder);

        // Assert
        var descriptor = descriptors.OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, int>>()
            .First(d => d.Name == "Mode");
        Assert.Equal(2, descriptor.GetSelectableValues(node).Count());
        Assert.Equal(1, descriptor.GetDefaultValue!(node));
        Assert.Equal(1, descriptor.GetValue!(node));
        descriptor.SetValue!(node, 2);
        Assert.Equal(2, descriptor.GetValue!(node));
        descriptor.ResetValue!(node);
        Assert.Equal(1, descriptor.GetValue!(node));
        Assert.True(descriptor.Visible!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithCircularSystemPropertyDependency_ThrowsInvalidOperationException()
    {
        // Arrange
        var first = new DataPortTreeNodeSystemProperty<string> { Name = "First", TypedValue = "a" };
        var second = new DataPortTreeNodeSystemProperty<string> { Name = "Second", TypedValue = "b" };
        first.DependentProperties!["Second"] = ["b"];
        second.DependentProperties!["First"] = ["a"];

        var node = CreateChildNode([first, second]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => GetDescriptors(node, clusterBuilder));
        Assert.Contains("Circular dependency", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPropertyDescriptors_IconDescriptor_ExposesWorkingValueAccessors()
    {
        // Arrange
        var node = CreateChildNode(availableIcons: ["icon-a", "icon-b"]);
        node.Icon = "icon-a";
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var iconDescriptor = GetDescriptors(node, clusterBuilder)
            .OfType<SelectionPropertyDescriptor<DataPortChildNodeModel, string>>()
            .First(descriptor => descriptor.Name == nameof(DataPortChildNodeModel.Icon));

        // Assert
        Assert.Equal("icon-a", iconDescriptor.GetDefaultValue!(node));
        Assert.Equal("icon-a", iconDescriptor.GetValue!(node));
        iconDescriptor.SetValue!(node, "icon-b");
        Assert.Equal("icon-b", iconDescriptor.GetValue!(node));
        iconDescriptor.ResetValue!(node);
        Assert.Equal("icon-a", iconDescriptor.GetValue!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithRegularSystemProperty_YieldsPropertyDescriptorWithWorkingAccessors()
    {
        // Arrange
        var node = CreateChildNode(
        [
            new DataPortTreeNodeSystemProperty<int>
            {
                Name = "Count",
                TypedDefaultValue = 3,
                TypedValue = 7,
            },
        ]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptor = GetDescriptors(node, clusterBuilder).First(d => d.Name == "Count");

        // Assert
        var typed = Assert.IsType<PropertyDescriptor<DataPortChildNodeModel, int>>(descriptor);
        Assert.Equal(7, typed.GetValue!(node));
        Assert.Equal(3, typed.GetDefaultValue!(node));
        typed.SetValue!(node, 11);
        Assert.Equal(11, typed.GetValue!(node));
        typed.ResetValue!(node);
        Assert.Equal(3, typed.GetValue!(node));
        Assert.True(typed.Visible!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithNumericCustomProperty_YieldsNumericDescriptor()
    {
        // Arrange
        var customProperty = new DataPortNodeModelCustomProperty
        {
            Category = "Custom",
            DefaultValue = 0,
            Id = Guid.NewGuid(),
            MaxValue = 100,
            MinValue = 0,
            Name = "Threshold",
            Reference = new PropertyReference { Id = "Threshold" },
            RuntimeType = typeof(int),
            Type = new PropertyType { Id = "Threshold", Name = "Threshold", UiControl = "NumericUpDown" },
            Value = 5,
        };
        var node = CreateChildNode([customProperty]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptor = GetDescriptors(node, clusterBuilder).First(d => d.Name == "Threshold");

        // Assert
        var typed = Assert.IsType<NumericPropertyDescriptor<DataPortChildNodeModel, int, int, int>>(descriptor);
        Assert.Equal(5, typed.GetValue!(node));
        Assert.Equal(0, typed.GetDefaultValue!(node));
        typed.SetValue!(node, 42);
        Assert.Equal(42, typed.GetValue!(node));
        typed.ResetValue!(node);
        Assert.Equal(0, typed.GetValue!(node));
        Assert.True(typed.Visible!(node));
    }

    [Fact]
    public void GetPropertyDescriptors_WithDefaultValuedCustomProperty_YieldsPropertyDescriptor()
    {
        // Arrange
        var customProperty = new DataPortNodeModelCustomProperty
        {
            Category = "Custom",
            DefaultValue = "default",
            Id = Guid.NewGuid(),
            Name = "FreeCustom",
            Reference = new PropertyReference { Id = "FreeCustom" },
            RuntimeType = typeof(string),
            Type = new PropertyType { DefaultValue = "default", Id = "FreeCustom", Name = "FreeCustom" },
            Value = "custom-value",
        };
        var node = CreateChildNode([customProperty]);
        using var clusterBuilder = new ClusterBuilder(Substitute.For<IDependencyResolver>());

        // Act
        var descriptor = GetDescriptors(node, clusterBuilder).First(d => d.Name == "FreeCustom");

        // Assert
        var typed = Assert.IsType<PropertyDescriptor<DataPortChildNodeModel, string>>(descriptor);
        Assert.Equal("custom-value", typed.GetValue!(node));
        Assert.Equal("default", typed.GetDefaultValue!(node));
        Assert.True(typed.HasValueDifferentFromDefaultValue!(node, "default"));
        typed.ResetValue!(node);
        Assert.False(typed.HasValueDifferentFromDefaultValue!(node, "default"));
    }
}
