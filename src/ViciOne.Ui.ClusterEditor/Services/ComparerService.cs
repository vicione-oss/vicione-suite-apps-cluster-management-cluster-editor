using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ViciOne.Ui.ClusterEditor.Extensions;
using ViciOne.Ui.ClusterEditor.Models.Comparer;

namespace ViciOne.Ui.ClusterEditor.Services;

public class ComparerService(IEnumerable<KnownComparerType> knownComparerTypes, ILogger<ComparerService> logger)
{
    private readonly Dictionary<Type, IComparer?> _comparerCache = [];
    private readonly Type[] _knownComparerTypes = knownComparerTypes.Select(t => t.Type).ToArray();

    private void CacheComparers(params Type[] types)
    {
        var comparerList = AppDomain.CurrentDomain.GetComparerInterfaces(logger);
        var availableComparers = new List<Type>();
        var searchTypes = new List<Type>();
        foreach (var type in types)
        {
            availableComparers.Clear();
            searchTypes.Clear();

            if (_comparerCache.ContainsKey(type))
                continue;

            if (type.IsPrimitive || type == typeof(string))
            {
                _comparerCache.Add(type, null);
                continue;
            }

            searchTypes.Add(type);

            if (type.BaseType is not null)
                searchTypes.Add(type.BaseType);

            searchTypes.AddRange(type.GetInterfaces());

            foreach (var searchType in searchTypes)
            {
                var comparerType = typeof(Comparer<>).MakeGenericType(searchType);
                availableComparers.AddRange(comparerList.Where(t => t.IsAssignableTo(comparerType)));
            }

            foreach (var typeComparer in availableComparers.Distinct())
            {
                var comparer = Activator.CreateInstance(typeComparer) as IComparer;
                if (comparer is not null)
                {
                    _comparerCache.Add(type, comparer);
                    break;
                }
            }

            if (!_comparerCache.ContainsKey(type))
                _comparerCache.Add(type, null);
        }
    }

    public IComparer? GetComparer(Type type)
    {
        if (_comparerCache.TryGetValue(type, out var comparer))
            return comparer;

        CacheComparers(type);
        return _comparerCache.TryGetValue(type, out comparer) ? comparer : null;
    }

    public void InitCache()
        => CacheComparers(_knownComparerTypes);
}
