using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Extensions;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Models;
using ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Services;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.PropertyValueEqualityComparer;

namespace ViciOne.Ui.ClusterEditor.Components.ContainerEditor.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddContainerEditor(this IServiceCollection services)
        => services.AddPropertyGrid();

    internal static IServiceCollection AddPropertyGrid(this IServiceCollection services)
    {
        services.AddPropertyGrid<ContainerEditorPropertyGridContext>()
            .WithPropertyDescriptorProvider<ContainerEditorChildContainerNodePropertyDescriptorProvider<ContainerEditorPropertyGridContext>>()
            .WithPropertyDescriptorProvider<ContainerEditorConnectorPropertyDescriptorProvider<ContainerEditorPropertyGridContext>>()

            .WithPropertyValueEqualityComparer<string, AlphaNumericPropertyValueEqualityComparer>();

        return services;
    }
}
