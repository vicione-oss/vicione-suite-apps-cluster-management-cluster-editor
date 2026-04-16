using Microsoft.AspNetCore.Mvc.Testing;

namespace ViciOne.Ui.ClusterEditor.EndToEnd.Tests.Infrastructure;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Needed because of required public constructor.")]
public sealed class ServerTestCollectionFixture : IDisposable
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory = new();

    public string ServerAddress => _webApplicationFactory.ClientOptions.BaseAddress.ToString();

    public ServerTestCollectionFixture()
    {
        _webApplicationFactory.UseKestrel(0);
        _webApplicationFactory.StartServer();
    }

    public void Dispose()
        => _webApplicationFactory.Dispose();
}
