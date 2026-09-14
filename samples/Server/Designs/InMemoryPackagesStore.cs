using System.IO.Abstractions;
using Sdk.Backend.Modules;
using Shared.Designs;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Core.Dataflow.DataModel.Generation;
using ViciOne.Core.Dataflow.Pooling;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Tree.Builder.Rules;

namespace Server.Designs;

internal sealed partial class InMemoryPackagesStore(
    IFileSystem fileSystem,
    ILogger<InMemoryPackagesStore> logger,
    IWorkspaceProvider<FakeBackendModule> workspaceProvider) : IPackagesStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<ClusterDependency, Content> _packages = [];
    private PackagesLoader? _packagesLoader;

    public void Initialize(IReadOnlyCollection<ClusterDependency> dependencies)
        => _packagesLoader = PackagesLoader.Create(dependencies, fileSystem.GetDependenciesPath(workspaceProvider), fileSystem);

    public bool TryAddPackage(ClusterDependency dependency, string directory, bool hide = false)
    {
        if (_packagesLoader is null)
            throw new InvalidOperationException("Packages loader is not initialized.");

        lock (_lock)
        {
            if (_packages.ContainsKey(dependency))
                return false;

            var designs = _packagesLoader.Load(directory);
            var rulesets = RulesetLoader.LoadRulesets(directory);

            _packages[dependency] = new(designs, rulesets, hide);
            return true;
        }
    }

    public bool TryAddPackage(ClusterDependency dependency, IReadOnlyCollection<Type> types, IReadOnlyCollection<Ruleset> rulesets, bool hide = false)
    {
        lock (_lock)
        {
            if (_packages.ContainsKey(dependency))
                return false;

            PoolingFactory poolingFactory = new([typeof(SumPooling).Assembly]);

            List<FunctionBlockDesign> functionBlockDesigns = [];

            foreach (var type in types)
            {
                var functionBlockDesignInfo = RuntimeFunctionBlockAssemblyAnalyzer.GetFunctionBlockDesign(type);
                var coreDesign = FunctionBlockDesignGenerator.GenerateFunctionBlockDesign(functionBlockDesignInfo, poolingFactory);

                functionBlockDesigns.Add(coreDesign);
            }

            _packages[dependency] = new(
                functionBlockDesigns.ToDictionary(d => d.Id),
                rulesets.Where(r => r.Root is not null).ToDictionary(r => r.Root!.Id),
                hide);
            return true;
        }
    }

    private sealed record Content(IReadOnlyDictionary<Guid, FunctionBlockDesign> Designs, IReadOnlyDictionary<string, Ruleset> Rulesets, bool Hide);
}
