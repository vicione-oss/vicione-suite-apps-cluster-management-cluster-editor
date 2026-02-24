using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Shared.Services;

public sealed class CultureService(IJSRuntime jsRuntime, NavigationManager navigationManager) : ICultureService
{
    private bool _isInitialized;

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentCulture;
    public IEnumerable<CultureInfo> SupportedCultures => [new("en-US"), new("de-DE")];

    public async Task Init(bool reload)
    {
        if (_isInitialized) return;

        var result = await jsRuntime.InvokeAsync<string>("ViciOne.Culture.getCurrentCulture");
        if (!string.IsNullOrEmpty(result))
        {
            var culture = new CultureInfo(result);
            if (culture.Name != CurrentCulture.Name)
            {
                CurrentCulture = culture;
                Update(reload);
            }
        }
        _isInitialized = true;
    }

    public async Task SetCurrentCulture(CultureInfo cultureInfo)
    {
        if (cultureInfo.Name != CurrentCulture.Name)
        {
            await jsRuntime.InvokeVoidAsync("ViciOne.Culture.setCurrentCulture", cultureInfo.Name);
            CurrentCulture = cultureInfo;
            Update(true);
        }
    }

    private void Update(bool reload)
    {
        CultureInfo.DefaultThreadCurrentCulture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;
        if (reload)
            navigationManager.NavigateTo(navigationManager.Uri.ToString(), true);
    }
}
