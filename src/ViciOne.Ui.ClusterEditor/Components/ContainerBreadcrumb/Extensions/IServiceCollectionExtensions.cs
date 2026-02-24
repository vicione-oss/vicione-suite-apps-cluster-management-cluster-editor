using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.Breadcrumb.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerBreadcrumb.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddContainerBreadcrumb(this IServiceCollection services)
        => services.AddBreadcrumb();
}
