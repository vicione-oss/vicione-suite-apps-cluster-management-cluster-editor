using System.Reflection;
using Sdk.Backend.Modules;

namespace Server.Designs;

internal sealed class FakeWorkspaceProvider() : IWorkspaceProvider<FakeBackendModule>
{
    public string Cache => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
    public string Home => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
}
