using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.Extensions;
using Shared.Services;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddDevExpressBlazor(configure => configure.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5);
builder.Services.AddLocalization();
builder.Services.AddClusterEditor(sp => sp.GetRequiredService<IRulesetProvider>());
builder.Services.AddCultureSupport();
builder.Services.AddScoped<IndexService>();

var host = builder.Build();

var cultureService = host.Services.GetRequiredService<ICultureService>();
if (cultureService is not null)
    await cultureService.Init(false);

await host.RunAsync();
