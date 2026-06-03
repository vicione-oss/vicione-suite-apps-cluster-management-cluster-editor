using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.Ui.Blazor.Components.CheckBox.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddUniversalInput(this IServiceCollection services)
    {
        services.TryAddScoped<BoxedNumericValueDescriptorBuilderProvider>();

        services.AddCheckBox();

        return services;
    }
}
