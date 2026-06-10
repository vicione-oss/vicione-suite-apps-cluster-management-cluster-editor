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
        using var enumerator = childContainer.GetEngines(out var containsUnassignedBlocks).GetEnumerator();

        if (!enumerator.MoveNext())
            return EngineTextNone;

        var first = enumerator.Current;

        if (enumerator.MoveNext() || containsUnassignedBlocks)
            return EngineTextMultiple;

        return GenerateLetter(GetEngineIndex(builder, first));
    }

    internal static string Get(IClusterBuilder builder, Cluster.Model.Engine? engine)
        => engine is null
        ? EngineTextNone
        : GenerateLetter(GetEngineIndex(builder, engine));

    internal static string Get(IClusterBuilder builder, FunctionBlock functionBlock)
        => Get(builder, functionBlock.Engine);

    internal static int GetEngineIndex(IClusterBuilder builder, Cluster.Model.Engine engine)
    {
        var index = 0;
        foreach (var cached in builder.Cache.EngineIds.Values)
        {
            if (cached == engine)
                return index;
            index++;
        }

        return -1;
    }
}
