using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ViciOne.Ui.ClusterEditor.Extensions;

public static partial class AppDomainExtensions
{
    private static IEnumerable<Type> GetAppDomainInterfaces<TInterface>(this AppDomain domain, ILogger? logger = null)
        => domain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .OrderBy(k => k.GetName().Name)
            .SelectMany(a => a.TryGetExportedTypes(logger))
            .Where(t => t.GetInterfaces().Contains(typeof(TInterface)) && t.IsClass && !t.IsNested);

    public static IEnumerable<Type> GetComparerInterfaces(this AppDomain domain, ILogger? logger = null)
        => domain.GetAppDomainInterfaces<IComparer>(logger);

    private static Type[] TryGetExportedTypes(this Assembly assembly, ILogger? logger = null)
    {
        try
        {
            // if assembly versions differ it might lead to types that are missing
            return assembly.GetExportedTypes();
        }
        catch (TypeLoadException ex)
        {
            if (logger is not null)
                WarnTypeLoadFailed(logger, ex, ex.TypeName, assembly.FullName ?? string.Empty);

            return [];
        }
    }

    [LoggerMessage(1, LogLevel.Warning, "Failed to load type {TypeName} from {AssemblyName}", EventName = "TypeLoadFailedWarning")]
    public static partial void WarnTypeLoadFailed(ILogger logger, Exception ex, string TypeName, string AssemblyName);
}
