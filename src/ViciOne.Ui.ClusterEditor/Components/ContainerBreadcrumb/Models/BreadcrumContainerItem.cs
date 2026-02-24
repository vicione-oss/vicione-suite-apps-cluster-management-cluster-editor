using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Models;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Models;

internal record BreadcrumbContainerItem : BreadcrumbItem
{
    internal required Container Container { get; init; }
}
