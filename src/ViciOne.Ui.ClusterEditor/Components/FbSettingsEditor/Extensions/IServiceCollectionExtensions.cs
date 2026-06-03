using Microsoft.Extensions.DependencyInjection;
using ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ComponentServices.FbSettingsEditor;

namespace ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor.Extensions;

internal static class IServiceCollectionExtensions
{
    public static IServiceCollection AddFbSettingsEditor(this IServiceCollection services)
    {
        services.AddScoped<IFbSettingsEditorRequest, FbSettingsEditorRequest>();

        services.AddUniversalInput();

        return services;
    }
}
