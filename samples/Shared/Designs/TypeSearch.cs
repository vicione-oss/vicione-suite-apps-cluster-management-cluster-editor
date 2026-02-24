using System.Reflection;
using System.Runtime.Loader;

namespace Shared.Designs;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Instances of this type are not compared to each other")]
public readonly struct TypeSearch
{
    private readonly IReadOnlyCollection<string> _assemblyPaths;
    private readonly List<Type> _interfaces = [];
    private readonly string _mainComponentPath;
    private readonly AssemblyLoadContext _parentContext;

    private TypeSearch(string mainComponentPath, IReadOnlyCollection<string> assemblyPaths, AssemblyLoadContext parentContext)
    {
        _mainComponentPath = mainComponentPath;
        _assemblyPaths = assemblyPaths;
        _parentContext = parentContext;
    }

    private static void CheckDirectoryExistence(string directory)
    {
        if (!Directory.Exists(directory))
            throw new ArgumentException("Could not find directory with assemblies.");
    }

    public static TypeSearch Create(string component, AssemblyLoadContext parentContext)
    {
        var directory = Path.GetDirectoryName(component) ?? string.Empty;
        CheckDirectoryExistence(directory);
        var assemblies = GetAssemblyPaths(directory);

        return new(component, assemblies, parentContext);
    }

    private static IEnumerable<TypeInfo> EnumeratePublicInstantiableTypes(IEnumerable<Assembly> assemblies)
        => assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Select(a => a.GetTypeInfo())
            .Where(t => !t.IsAbstract && t.IsClass);

    private static List<string> GetAssemblyPaths(string directory)
        => [.. Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly)];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Argumentausnahmen korrekt instanziieren", Justification = "Es handelt sich um Parameter eines unbekannten Typs.")]
    private List<AssemblyName> IdentifyAssembliesToLoad(IEnumerable<TypeInfo> availableTypes)
    {
        availableTypes = [.. availableTypes];
        List<string> interfaces = [];
        List<string> genericInterfaces = [];

        foreach (var @interface in _interfaces)
        {
            if (@interface.IsGenericTypeDefinition)
                genericInterfaces.Add(@interface.FullName ?? throw new ArgumentException("Cannot be null or empty.", "FullName"));
            else
                interfaces.Add(@interface.FullName ?? throw new ArgumentException("Cannot be null or empty.", "FullName"));
        }

        List<AssemblyName> assemblies =
        [
            .. availableTypes
                .Where(t => t.ImplementedInterfaces.Any(t => interfaces.Contains(t.FullName ?? string.Empty)))
                .Select(t => t.Assembly)
                .Distinct()
                .Select(a => a.GetName()),
            .. availableTypes
                .Where(t => TypeImplementsGenericInterfaceTypes(t, genericInterfaces))
                .Select(t => t.Assembly)
                .Distinct()
                .Select(a => a.GetName()),
        ];
        return assemblies;

        static bool TypeImplementsGenericInterfaceTypes(TypeInfo t, IEnumerable<string> genericTypes)
            => t.ImplementedInterfaces.Any(i => i.IsGenericType && genericTypes.Contains(i.GetGenericTypeDefinition().FullName));
    }

    public IReadOnlyCollection<Assembly> LoadAssemblies(AssemblyLoadContext assemblyLoadContext)
    {
        using ReflectionContext reflectionContext = new(_mainComponentPath, _assemblyPaths, _parentContext);
        reflectionContext.LoadAssemblies(_assemblyPaths);
        var availableTypes = EnumeratePublicInstantiableTypes(reflectionContext.Assemblies);
        var assemblies = IdentifyAssembliesToLoad(availableTypes);

        return [.. LoadAssemblies(assemblyLoadContext, assemblies)];
    }

    private static IEnumerable<Assembly> LoadAssemblies(AssemblyLoadContext assemblyLoadContext, IReadOnlyCollection<AssemblyName> assemblies)
    {
        foreach (var assembly in assemblies)
            yield return assemblyLoadContext.LoadFromAssemblyName(assembly);
    }

    public TypeSearch RegisterInterfacesToLoad(IEnumerable<Type> types)
    {
        _interfaces.AddRange(types);
        return this;
    }
}
