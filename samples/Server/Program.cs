#pragma warning disable CA1506
// Warning	CA1506	'<Main>$' is coupled with '43' different types from '33' different namespaces. Rewrite or refactor the code to decrease its class coupling below '41'.

using BlazorWasm.Client;
using Server.Designs;
using Shared.Designs;
using Shared.Extensions;
using Shared.Persistence.Extensions;
using Shared.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot",
});

var useWebassembly = builder.Configuration.GetValue<bool>("RenderWasm");

builder.Services.AddAntiforgery();

if (useWebassembly)
{
    builder.Services
        .AddRazorComponents()
        .AddInteractiveWebAssemblyComponents();
}
else
{
    builder.Services
        .AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddHubOptions(configure => configure.MaximumReceiveMessageSize = 50 * 1024 * 1024);

    builder.Services.AddHostedService<DesignLoader>();
}

builder.Services.AddLocalization();

builder.Services.AddDevExpressBlazor();
builder.WebHost.UseStaticWebAssets();

builder.Services.AddClusterDependenciesSupport();
builder.Services.AddClusterEditor(s => (IRulesetProvider)s.GetRequiredService<IPackagesStore>());
builder.Services.AddCultureSupport();
builder.Services.AddPersistence();
builder.Services.AddScoped<IndexService>();
builder.Services.AddSingleton(new RenderModeProvider(useWebassembly));

var app = builder.Build();

app.MapStaticAssets();
app.UseRouting();
app.UseAntiforgery();

if (useWebassembly)
{
    app.UseWebAssemblyDebugging();
    app.MapRazorComponents<App>()
        .AddInteractiveWebAssemblyRenderMode();
}
else
{
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
}

await app.RunAsync();

#pragma warning restore CA1506
