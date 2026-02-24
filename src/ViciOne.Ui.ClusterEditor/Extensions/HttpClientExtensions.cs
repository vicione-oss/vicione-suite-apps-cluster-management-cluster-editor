using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class HttpClientExtensions
{
    private const string EmptyVersion = "n/a";

    public static async Task<string> TryRetrieveEditorVersionAsync(this HttpClient httpClient, string baseUri)
    {
        try
        {
            var assemblyName = typeof(HttpClientExtensions).Assembly.GetName().Name;
            var res = await httpClient!.GetAsync(new Uri($"{baseUri}_content/{assemblyName}/data/version.json"));
            if (!res.IsSuccessStatusCode)
                return EmptyVersion;

            var resDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync());
            if (resDict is null || resDict.Count == 0 || !resDict.TryGetValue("Version", out var version))
                return EmptyVersion;

            return version;
        }
        catch
        {
            return EmptyVersion;
        }
    }
}
