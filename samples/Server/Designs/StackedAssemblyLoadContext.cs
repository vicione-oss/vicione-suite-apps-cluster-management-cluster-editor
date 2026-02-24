using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Shared.Designs;
using ViciOne.Cluster.Model;

namespace Server.Designs;

internal sealed class StackedAssemblyLoadContext : AssemblyLoadContext
{
    private readonly ConcurrentDictionary<string, Assembly?> _managedAssembliesCache = [];
    private readonly IReadOnlyDictionary<string, AssemblyDependencyResolver> _resolverMap;
    private readonly IReadOnlyCollection<Assembly> _shared;
    private readonly Stack<IntPtr> _unmanagedAssemblies = new();
    private readonly ConcurrentDictionary<string, IntPtr> _unmanagedAssembliesCache = [];

    internal StackedAssemblyLoadContext(string name, IReadOnlyCollection<Assembly> shared, IReadOnlyCollection<ClusterDependency> dependencies, string packagesDirectory, IFileSystem fileSystem) : base(name)
    {
        _shared = shared;
        _resolverMap = ClusterAssembliesResolver.Resolve(dependencies, packagesDirectory, fileSystem);
        Unloading += _ =>
        {
            _managedAssembliesCache.Clear();
            _unmanagedAssembliesCache.Clear();

            while (_unmanagedAssemblies.Count != 0)
                NativeLibrary.Free(_unmanagedAssemblies.Pop());
        };
    }

    // https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading#library-name-variations
    private static string[] GetPlatformSpecificCandidates(string name)
    {
        if (OperatingSystem.IsLinux())
        {
            return
            [
                name,
                $"lib{name}",
                $"{name}.so",
                $"lib{name}.so"
            ];
        }
        else if (OperatingSystem.IsWindows())
        {
            return [name];
        }
        throw new PlatformNotSupportedException();
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name?.StartsWith("System.Runtime", StringComparison.Ordinal) is true)
            return null;

        if (_managedAssembliesCache.TryGetValue(assemblyName.FullName, out var cachedAssembly))
            return cachedAssembly;

        // use shared assemblies from module context 
        var assemblyExists = _shared.Any(a => a.GetName().Name == assemblyName.Name);
        if (assemblyExists)
            return _managedAssembliesCache.GetOrAdd(assemblyName.FullName, (Assembly?)null);

        // already loaded in default context? then let it be resolved there (e.g. netstandard)
        if (Default.Assemblies.Any(a => a.GetName().Name == assemblyName.Name))
            return _managedAssembliesCache.GetOrAdd(assemblyName.FullName, (Assembly?)null);

        if (_resolverMap.TryGetValue(assemblyName.Name ?? string.Empty, out var resolver))
        {
            var path = resolver.ResolveAssemblyToPath(assemblyName);
            if (path is not null)
                return _managedAssembliesCache.GetOrAdd(assemblyName.FullName, _ => LoadFromAssemblyPath(path));
        }

        return _managedAssembliesCache.GetOrAdd(assemblyName.FullName, (Assembly?)null);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var filenameCandidates = GetPlatformSpecificCandidates(Path.GetFileNameWithoutExtension(unmanagedDllName));

        foreach (var filename in filenameCandidates)
        {
            if (_resolverMap.TryGetValue(filename, out var resolver))
            {
                var path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

                if (path is not null && NativeLibrary.TryLoad(path, out var handle))
                {
                    if (_unmanagedAssembliesCache.TryAdd(unmanagedDllName, handle))
                    {
                        _unmanagedAssemblies.Push(handle);
                        return handle;
                    }
                    NativeLibrary.Free(handle);
                    return _unmanagedAssembliesCache.TryGetValue(unmanagedDllName, out var result) ? result : IntPtr.Zero;
                }
            }
        }

        return IntPtr.Zero;
    }
}
