using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Extensions;
using Xunit;
using Container = ViciOne.Cluster.Model.Container;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerBreadcrumb.Extensions;

public sealed class ContainerExtensions
{
    private static T Create<T>(string name, List<ChildContainer>? childContainers = null) where T : Container, new()
    {
        var container = new T { Containers = childContainers ?? [], Name = name };
        foreach (var childContainer in childContainers ?? [])
            childContainer.Parent = container;

        return container;
    }

    [Fact]
    public void Returns_converted_ancestors_and_self()
    {
        // Arrange
        var child = Create<ChildContainer>("child");
        var target = Create<ChildContainer>("current", [child]);
        var sibling = Create<ChildContainer>("sibling");
        var parent = Create<ChildContainer>("parent", [target, sibling]);
        var root = Create<Container>("root", [parent]);

        // Act
        var result = target.ToBreadcrumbContainerItem();

        // Assert
        Assert.Equal(target.Name, result.Name);
        Assert.Equal(parent.Name, result.Parent!.Name);
        Assert.Equal(root.Name, result.Parent!.Parent!.Name);
    }

    [Fact]
    public void Returns_only_converted_root_when_given_root()
    {
        // Arrange
        var root = Create<Container>("root");

        // Act
        var result = root.ToBreadcrumbContainerItem();

        // Assert
        Assert.Equal(root.Name, result.Name);
    }
}
