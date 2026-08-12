using System;
using System.Linq;
using ViciOne.TreeBuilder.NodeTypes;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Extensions;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Models;
using ViciOne.Ui.ClusterEditor.Sections.DataPorts.Services;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Sections.DataPorts.Services;

public sealed class DataPortChildNodeModelFactoryTests
{
    private static DataPortRootNodeModel CreateRoot()
        => new()
        {
            Builder = new TreeBuilder.TreeBuilder(Resources.TestResources.MqttRuleset),
            Name = "Root",
        };

    [Fact]
    public void CreateDataPortChildNodeModel_WithValidDescriptor_CreatesNodeFromNodeType()
    {
        // Arrange
        var root = CreateRoot();
        var descriptor = root.GetPossibleChildNodes().First(d => d.Children.Count == 0);
        var nodeType = root.Builder.NodeTypes[descriptor.NodeReference.Id];

        // Act
        var result = DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, root);

        // Assert
        Assert.Equal(nodeType.Name, result.Name);
        Assert.Same(root, result.RootNode);
        Assert.Same(root, result.Parent);
        Assert.Equal(descriptor.NodeReference, result.NodeReference);
        Assert.Equal(nodeType.Icons, result.AvailableIcons);
        Assert.Equal(nodeType.Icons.FirstOrDefault(), result.Icon);
        Assert.Equal(nodeType.NameIsReadOnly, result.NameIsReadOnly);
    }

    [Fact]
    public void CreateDataPortChildNodeModel_WithUnknownNodeReference_ThrowsInvalidOperationException()
    {
        // Arrange
        var root = CreateRoot();
        var descriptor = new DataPortChildNodeContextMenuDescriptor
        {
            IconName = "icon",
            Name = "Unknown",
            NodeReference = new NodeReference { Id = "does-not-exist" },
            ParentNode = root,
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => DataPortChildNodeModelFactory.CreateDataPortChildNodeModel(descriptor, root));
        Assert.Contains("does-not-exist", exception.Message, StringComparison.Ordinal);
    }
}
