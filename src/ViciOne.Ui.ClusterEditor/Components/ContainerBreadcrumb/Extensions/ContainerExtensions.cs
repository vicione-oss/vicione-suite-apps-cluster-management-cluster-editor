using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Models;
using ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Models;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Extensions;

internal static class ContainerExtensions
{
    private static void AddChildren(BreadcrumbItem target, IEnumerable<Container> children, Container? except = null)
    {
        foreach (var child in children)
        {
            if (child.Id == except?.Id)
                continue;

            target.AddChild(new BreadcrumbContainerItem { Container = child, Name = child.Name });
        }
    }

    internal static BreadcrumbContainerItem ToBreadcrumbContainerItem(this Container currentContainer, string? rootName = null)
    {
        var selectedItem = new BreadcrumbContainerItem { Container = currentContainer, Name = currentContainer.Name };
        var currentItem = selectedItem;

        AddChildren(currentItem, currentContainer.Containers);

        // while there is a parent...
        while (currentContainer is ChildContainer current)
        {
            var parentContainer = current.Parent;
            var parentItem = new BreadcrumbContainerItem { Container = parentContainer, Name = parentContainer.Name };

            // add the current item & all other siblings as child
            parentItem.AddChild(currentItem);
            AddChildren(parentItem, parentContainer.Containers, currentContainer);

            // prepare for next iteration
            currentContainer = parentContainer;
            currentItem = parentItem;
        }

        // the last visited item is the root container, which is displayed with the name of its dataflow
        if (!string.IsNullOrWhiteSpace(rootName))
            currentItem.Name = rootName;

        return selectedItem;
    }
}
