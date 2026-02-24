using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.ClusterEditor.Sections.Library.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Services;

public sealed class LibraryService
{
    public IEnumerable<LibraryEntry>? DraggingEntries { get; set; } = null!;
    public IEnumerable<LibraryEntry> LibraryEntries { get; private set; } = [];

    public event Action? DragEnded;
    public event Action? DragStarted;
    public event Action? EntriesChanged;
    public event Action<Guid>? FunctionBlockCreationRequested;

    private static List<LibraryEntry> CreateEntries(Dictionary<string, List<FunctionBlockDesign>> functionBlockDesignNamespaces, LibraryEntry? parent = null)
    {
        var result = new List<LibraryEntry>();

        var orderedFbNs = functionBlockDesignNamespaces
            .Where(ns => !string.IsNullOrWhiteSpace(ns.Key))
            .OrderBy(ns => ns.Key)
            .ToList();
        orderedFbNs.AddRange(functionBlockDesignNamespaces.Where(ns => string.IsNullOrEmpty(ns.Key)));

        foreach (var namespaceEntry in orderedFbNs)
        {
            if (string.IsNullOrWhiteSpace(namespaceEntry.Key))
            {
                foreach (var entry in namespaceEntry.Value.OrderBy(d => d.Name))
                {
                    result.Add(new()
                    {
                        FullName = parent is not null ? $"{parent.FullName}.{entry.Name}" : entry.Name,
                        Name = entry.Name,
                        Parent = parent,
                        UniqueId = entry.Id
                    });
                }
            }
            else
            {
                var entry = new LibraryEntry()
                {
                    FullName = parent is not null ? $"{parent.FullName}.{namespaceEntry.Key}" : namespaceEntry.Key,
                    IsStructureNode = true,
                    Name = namespaceEntry.Key,
                    Parent = parent,
                    UniqueId = Guid.NewGuid(),
                };

                entry.Children.AddRange(CreateEntries(GroupByNamespace(namespaceEntry.Value, entry.FullName), entry));
                result.Add(entry);
            }
        }

        return result;
    }

    public void CreateLibraryEntries(IEnumerable<FunctionBlockDesign> functionBlockDesigns)
    {
        LibraryEntries = CreateEntries(GroupByNamespace(functionBlockDesigns, string.Empty));
        EntriesChanged?.Invoke();
    }

    private static Dictionary<string, List<FunctionBlockDesign>> GroupByNamespace(IEnumerable<FunctionBlockDesign> designs, string parentNs)
    {
        var result = new Dictionary<string, List<FunctionBlockDesign>>();
        foreach (var design in designs.OrderBy(d => $"{d.Namespace}.{d.Name}"))
        {
            var ns = design.Namespace[parentNs.Length..]
                .TrimStart('.')
                .Split('.')[0];

            if (!result.ContainsKey(ns))
                result.Add(ns, []);

            result[ns].Add(design);
        }

        return result;
    }

    public void InvokeDragEnded()
        => DragEnded?.Invoke();

    public void InvokeDragStarted()
        => DragStarted?.Invoke();

    public void RequestFunctionBlockCreation(Guid designId)
        => FunctionBlockCreationRequested?.Invoke(designId);
}
