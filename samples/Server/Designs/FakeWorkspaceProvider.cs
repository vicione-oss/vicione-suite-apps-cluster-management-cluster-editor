using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sdk.Backend.Modules;

namespace Server.Designs;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used in testing")]
internal sealed class FakeWorkspaceProvider() : IWorkspaceProvider<FakeBackendModule>
{
    public string Cache => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    public string Home => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
}
