using System.Linq;
using ViciOne.Cluster.Builder.Abstractions;
using ViciOne.Cluster.Model;
using ViciOne.Cluster.Model.Extensions;

namespace ViciOne.Ui.ClusterEditor.Helpers;

// TODO: EngineDisplayId - hier ist eine robuste Lösung zu finden
// Diese simple Variante geht u.a. kaputt wenn man zur Diagrammlebenszeit die Engines editiert
// Ebenso kann es vorkommen, dass die Engines bei verschiedenen Ständen unterschiedliche Abkürzungen bekommen
internal static class EngineDisplayText
{
    private const string IdChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal const string EngineTextError = "---";
    internal const string EngineTextMultiple = "...";
    internal const string EngineTextNone = "";
    internal const string EngineTextTooBig = "*";

    private static string GenerateLetter(int index)
    {
        if (index < 0)
            return EngineTextError;
        else if (index >= IdChars.Length)
            return EngineTextTooBig;
        else
            return IdChars[index].ToString();
    }

    internal static string Get(IClusterBuilder builder, ChildContainer childContainer)
    {
        var engines = childContainer.GetEngines(out var containsUnassignedBlocks).ToArray();

        if (engines.Length == 0)
            return EngineTextNone;
        else if (engines.Length == 1 && !containsUnassignedBlocks)
            return GenerateLetter(GetEngineIndex(builder, engines.First()));
        else
            return EngineTextMultiple;
    }

    internal static string Get(IClusterBuilder builder, Cluster.Model.Engine? engine)
        => engine is null
        ? EngineTextNone
        : GenerateLetter(GetEngineIndex(builder, engine));

    internal static string Get(IClusterBuilder builder, FunctionBlock functionBlock)
        => Get(builder, functionBlock.Engine);

    internal static int GetEngineIndex(IClusterBuilder builder, Cluster.Model.Engine engine)
        => builder.Cache.EngineGuids.Values.ToList().IndexOf(engine);
}
