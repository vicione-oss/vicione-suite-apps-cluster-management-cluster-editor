using System.IO.Abstractions;
using Sdk.Backend.Modules;

namespace Server.Designs;

internal static class IFileSystemExtensions
{
    private const string DependenciesDirectory = "Dependencies";

    public static string GetDependenciesPath(this IFileSystem fileSystem, IWorkspaceProvider<FakeBackendModule> ws)
        => fileSystem.Path.Combine(ws.Cache, DependenciesDirectory);
}
