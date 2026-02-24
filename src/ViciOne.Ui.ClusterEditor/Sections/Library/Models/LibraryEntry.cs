using System;
using System.Collections.Generic;
using ViciOne.Ui.ClusterEditor.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Models;

public sealed class LibraryEntry : IParentNode<LibraryEntry>
{
    public List<LibraryEntry> Children { get; } = [];
    IEnumerable<LibraryEntry> IParentNode<LibraryEntry>.Children
    {
        get => Children;
        set
        {
            Children.Clear();
            Children.AddRange(value);
        }
    }

    internal string FullName { get; init; } = string.Empty;
    internal bool IsStructureNode { get; init; }
    internal string Name { get; init; } = string.Empty;
    internal LibraryEntry? Parent { get; init; }
    internal Guid UniqueId { get; init; }
}
