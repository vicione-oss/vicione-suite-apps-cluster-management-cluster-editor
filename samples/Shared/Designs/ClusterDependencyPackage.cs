using System.Diagnostics;
using ViciOne.Cluster.Model;
using ViciOne.Core.Dataflow.DataModel;

namespace Shared.Designs;

[DebuggerDisplay("{Dependency.Name} v{Dependency.Version}, {FunctionBlockDesigns.Count} FunctionBlockDesigns")]
public sealed record ClusterDependencyPackage(ClusterDependency Dependency, List<FunctionBlockDesign> FunctionBlockDesigns, List<string> DataPortRootChildNodeIds);
