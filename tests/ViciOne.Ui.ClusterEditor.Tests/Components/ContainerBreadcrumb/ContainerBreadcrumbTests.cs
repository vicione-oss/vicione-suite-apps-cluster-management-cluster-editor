using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Components;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.TestHelpers;
using Xunit;
using Container = ViciOne.Cluster.Model.Container;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.ContainerBreadcrumb;

public sealed class ContainerBreadcrumbTests : IAsyncDisposable
{
    private BunitContext Ctx { get; } = new();

    private static T Create<T>(string name, List<ChildContainer>? childContainers = null) where T : Container, new()
    {
        var container = new T { Containers = childContainers ?? [], Name = name };
        foreach (var childContainer in childContainers ?? [])
            childContainer.Parent = container;

        return container;
    }

    public ValueTask DisposeAsync()
        => Ctx.DisposeAsync();

    private async Task<Dataflow> Init(Container current)
    {
        Ctx.ComponentFactories.AddStub<Breadcrumb>();
        Ctx.SetupDatastore();
        Ctx.SetupDiagramService();
        Ctx.CreateDiagramInstance();
        Ctx.JSInterop.Setup<int[]>("ViciOne.Diagram.BlockNode.measureNameFieldHeights", _ => true).SetResult([0]);

        var datastore = Ctx.Services.GetRequiredService<IDatastore>();
        var diagramService = Ctx.Services.GetRequiredService<DiagramService>();


        var root = current;
        if (current is ChildContainer childContainer)
        {
            root = childContainer.Parent;
            while (root is ChildContainer child)
                root = child.Parent;
        }

        using var builder = BuilderFactory.Create();
        var dataflow = builder.Cluster.Dataflows.First();
        dataflow.Root = root;

        await datastore.Load(builder, diagramService, CancellationToken.None);
        await datastore.LoadContainer(current, diagramService);

        return dataflow;
    }

    [Fact]
    public async Task Shows_only_root_when_selected_container_is_root()
    {
        // Arrange
        var root = Create<Container>("root");

        var dataflow = await Init(root);

        // Act
        var component = Ctx.Render<ClusterEditor.Components.ContainerBreadcrumb.ContainerBreadcrumb>();

        var breadcrumbStub = component.FindComponent<Stub<Breadcrumb>>();
        var currentBreadcrumbItem = breadcrumbStub.Instance.Parameters.Get(p => p.CurrentItem);

        // Assert
        Assert.Equal(currentBreadcrumbItem.Name, dataflow.Name);
    }

    [Fact]
    public async Task Shows_selected_container_and_all_ancestors_when_selected_container_is_not_root()
    {
        // Arrange
        var child = Create<ChildContainer>("child");
        var current = Create<ChildContainer>("current", [child]);
        var sibling = Create<ChildContainer>("sibling");
        var parent = Create<ChildContainer>("parent", [current, sibling]);
        Create<Container>("root", [parent]);

        var dataflow = await Init(current);

        // Act
        var component = Ctx.Render<ClusterEditor.Components.ContainerBreadcrumb.ContainerBreadcrumb>();

        var breadcrumbStub = component.FindComponent<Stub<Breadcrumb>>();
        var currentBreadcrumbItem = breadcrumbStub.Instance.Parameters.Get(p => p.CurrentItem);

        // Assert
        Assert.Equal(currentBreadcrumbItem.Name, current.Name);
        Assert.Equal(currentBreadcrumbItem.Parent!.Name, parent.Name);
        Assert.Equal(currentBreadcrumbItem.Parent!.Parent!.Name, dataflow.Name);
    }
}
