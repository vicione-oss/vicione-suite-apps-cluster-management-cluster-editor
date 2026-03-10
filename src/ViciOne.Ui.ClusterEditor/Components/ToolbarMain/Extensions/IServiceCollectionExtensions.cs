using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.Blazor.Components.Toolbar.Extensions;

namespace ViciOne.Ui.ClusterEditor.Components.ToolbarMain.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMainToolbar(this IServiceCollection services)
        => services.AddToolbar();
}
