using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Cluster.Builder;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Components;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using Container = ViciOne.Cluster.Model.Container;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerBreadcrumb;

public sealed class ContainerBreadcrumbTests : IDisposable
{
    private Bunit.TestContext Ctx { get; } = new();

    private static T Create<T>(string name, List<ChildContainer>? childContainers = null) where T : Container, new()
    {
        var container = new T { Containers = childContainers ?? [], Name = name };
        foreach (var childContainer in childContainers ?? [])
            childContainer.Parent = container;

        return container;
    }

    public void Dispose()
        => Ctx.Dispose();

    private async Task Init(Container current)
    {
        Ctx.ComponentFactories.AddStub<Breadcrumb>();
        Ctx.SetupDatastore();
        Ctx.SetupDiagramService();
        Ctx.CreateDiagramInstance();
        Ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var datastore = Ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = Ctx.Services.GetRequiredService<DiagramService>();

        // Create a function block design for testing
        var dependencyResolver = Substitute.For<IDependencyResolver>();
        var designId = Guid.NewGuid();
        var design = new FunctionBlockDesign
        {
            DefaultCycleFrequency = 10,
            Id = designId,
            Name = "TestDesign"
        };
        dependencyResolver.ResolveFunctionBlockDesign(designId).Returns(design);

        var root = current;
        if (current is ChildContainer childContainer)
        {
            root = childContainer.Parent;
            while (root is ChildContainer child)
                root = child.Parent;
        }

        using var builder = new ClusterBuilder(dependencyResolver);
        builder.Cluster.Dataflows.First().Root = root;

        await datastore.Load(builder, diagramService, CancellationToken.None);
        await datastore.LoadContainer(current, diagramService);
    }

    [Fact]
    public async Task Shows_only_root_when_selected_container_is_root()
    {
        // Arrange
        var root = Create<Container>("root");

        await Init(root);

        // Act
        var component = Ctx.RenderComponent<ClusterEditor.Components.ContainerBreadcrumb.ContainerBreadcrumb>();

        var breadcrumbStub = component.FindComponent<Stub<Breadcrumb>>();
        var currentBreadcrumbItem = breadcrumbStub.Instance.Parameters.Get(p => p.CurrentItem);

        // Assert
        Assert.Equal(currentBreadcrumbItem.Name, root.Name);
    }

    [Fact]
    public async Task Shows_selected_container_and_all_ancestors_when_selected_container_is_not_root()
    {
        // Arrange
        var child = Create<ChildContainer>("child");
        var current = Create<ChildContainer>("current", [child]);
        var sibling = Create<ChildContainer>("sibling");
        var parent = Create<ChildContainer>("parent", [current, sibling]);
        var root = Create<Container>("root", [parent]);

        await Init(current);

        // Act
        var component = Ctx.RenderComponent<ClusterEditor.Components.ContainerBreadcrumb.ContainerBreadcrumb>();

        var breadcrumbStub = component.FindComponent<Stub<Breadcrumb>>();
        var currentBreadcrumbItem = breadcrumbStub.Instance.Parameters.Get(p => p.CurrentItem);

        // Assert
        Assert.Equal(currentBreadcrumbItem.Name, current.Name);
        Assert.Equal(currentBreadcrumbItem.Parent!.Name, parent.Name);
        Assert.Equal(currentBreadcrumbItem.Parent!.Parent!.Name, root.Name);
    }
}
