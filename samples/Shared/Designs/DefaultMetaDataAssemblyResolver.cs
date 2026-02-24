using System.Reflection;
using System.Runtime.Loader;

namespace Shared.Designs;

public class DefaultMetaDataAssemblyResolver(string mainComponentPath, IEnumerable<string> paths, AssemblyLoadContext parentContext) : MetadataAssemblyResolver
{
    private readonly Dictionary<string, string> _paths = paths.ToDictionary(p => Path.GetFileNameWithoutExtension(p), p => p, StringComparer.OrdinalIgnoreCase);
    private readonly AssemblyDependencyResolver _resolver = new(mainComponentPath);

    internal static Assembly LoadWithStreamFromPath(MetadataLoadContext context, string path)
    {
        using var file = File.OpenRead(path);
        return context.LoadFromStream(file);
    }

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        _paths.TryGetValue(assemblyName.Name ?? string.Empty, out var path);
        path ??= ResolveAssemblyToPath(assemblyName);
        path ??= parentContext.LoadFromAssemblyName(assemblyName)?.Location;
        path ??= Assembly.Load(assemblyName).Location;

        if (string.IsNullOrEmpty(path))
            return null;
        return LoadWithStreamFromPath(context, path);

        string? ResolveAssemblyToPath(AssemblyName assemblyName)
        {
            var path = _resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrEmpty(path))
                return path;
            return null;
        }
    }
}
