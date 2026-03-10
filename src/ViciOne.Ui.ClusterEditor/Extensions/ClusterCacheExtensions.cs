using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class ClusterCacheExtensions
{
    public static IEnumerable<Cluster.Model.Engine> GetUnusedEngines(this IClusterCache cache)
        => cache.EngineGuids.Values.Where(engine =>
            !cache.GetDataPorts(engine).Any() &&
            !cache.GetFunctionBlocks(engine).Any());

    public static IEnumerable<Cluster.Model.Engine> GetUsedEngines(this IClusterCache cache, Dataflow dataflow)
    {
        foreach (var engine in cache.EngineGuids.Values)
        {
            // HACK: Da eine Engine nur in einem Dataflow genutzt werden kann, können wir zur nächsten Engine springen,
            //       sobald wir ein Element finden, was im angefragten Dataflow steckt.
            if (cache.GetDataPorts(engine).Any(dataPort => dataPort.Dataflow == dataflow) ||
                cache.GetFunctionBlocks(engine).Any(functionBlock => cache.GetDataflow(functionBlock) == dataflow))
            {
                yield return engine;
                continue;
            }
        }
    }
}
