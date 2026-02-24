using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.Loader;
using Shared.Designs;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;

namespace Server.Designs;

internal sealed class PackagesLoader
{
    private static readonly Type[] s_relevantInterfaces =
    [
        typeof(ViciOne.Core.Contracts.Runtime.IRuntimeFunctionBlock),
        typeof(ViciOne.Core.Contracts.Runtime.IRuntimeDriver),
        typeof(ViciOne.Core.Contracts.DataModel.IAggregatingPooling),
        typeof(ViciOne.Core.Contracts.DataModel.IValidator),
        typeof(ViciOne.Core.Contracts.DataModel.ITypeConversion),
    ];
    private static readonly Assembly[] s_shared =
    [
        typeof(ViciOne.Core.Contracts.DataModel.IConnectorDesign).Assembly,             // ViciOne.Core.Contracts
        typeof(ViciOne.Core.Dataflow.NameValidator).Assembly,                           // ViciOne.Core.Dataflow
        typeof(ViciOne.Core.Runtime.Attributes.FunctionBlockDesignAttribute).Assembly,  // ViciOne.Core.Runtime
        typeof(ViciOne.Engine.DefaultValidators.StringLengthValidator).Assembly,        // ViciOne.Engine.DefaultValidators
        typeof(AndPooling).Assembly,                                                    // ViciOne.Engine.DefaultPoolings
        typeof(ILoggerFactory).Assembly,                                                // Microsoft.Extensions.Logging.Abstractions
    ];

    private readonly IFileSystem _fileSystem;
    private readonly AssemblyLoadContext _parentContext;

    internal StackedAssemblyLoadContext Context { get; }

    private PackagesLoader(StackedAssemblyLoadContext context, AssemblyLoadContext parentContext, IFileSystem fileSystem)
    {
        Context = context;
        _parentContext = parentContext;
        _fileSystem = fileSystem;
    }

    internal static PackagesLoader Create(IReadOnlyCollection<ClusterDependency> dependencies, string packagesDirectory, IFileSystem fileSystem)
        => new(
            new(Constants.PackagesContext, s_shared, dependencies, packagesDirectory, fileSystem),
            AssemblyLoadContext.Default,
            fileSystem);

    internal IReadOnlyDictionary<Guid, FunctionBlockDesign> Load(string directory)
    {
        var depsFile = _fileSystem.Directory.GetFiles(directory, "*.deps.json", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"Could not find main component in directory '{directory}'.");
        var mainComponentPath = depsFile.Replace(".deps.json", ".dll", StringComparison.OrdinalIgnoreCase);
        var assemblies = TypeSearch.Create(mainComponentPath, _parentContext)
            .RegisterInterfacesToLoad(s_relevantInterfaces)
            .LoadAssemblies(Context);

        PoolingFactory poolingFactory = new(assemblies.Append(typeof(SumPooling).Assembly));
        var functionBlockDesignInfos = RuntimeFunctionBlockAssemblyAnalyzer
            .GetFunctionBlockDesigns(assemblies)
            .ToArray();
        var coreDesigns = FunctionBlockDesignGenerator
            .GenerateFunctionBlockDesigns(functionBlockDesignInfos, poolingFactory)
            .ToDictionary(d => d.Id);

        return coreDesigns;
    }
}
