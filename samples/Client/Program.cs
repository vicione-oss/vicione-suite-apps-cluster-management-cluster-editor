using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shared.ClusterManagement.Extensions;
using Shared.Debugging.Extensions;
using Shared.Extensions;
using Shared.Persistence.Extensions;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddDevExpressBlazor(configure => configure.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5);
builder.Services.AddLocalization();
builder.Services.AddClusterEditor(sp => sp.GetRequiredService<IRulesetProvider>());
builder.Services.AddClusterManagement();
builder.Services.AddDebugging();
builder.Services.AddCultureSupport();
builder.Services.AddPersistence();

var host = builder.Build();

await host.RunAsync();
