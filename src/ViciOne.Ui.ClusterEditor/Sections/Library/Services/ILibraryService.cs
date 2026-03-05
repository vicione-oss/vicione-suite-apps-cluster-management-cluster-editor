using System;
using System.Collections.Generic;
using ViciOne.Core.Dataflow.DataModel;
using ViciOne.Ui.ClusterEditor.Sections.Library.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Library.Services;

internal interface ILibraryService
{
    IEnumerable<LibraryEntry>? DraggingEntries { get; set; }
    IEnumerable<LibraryEntry> LibraryEntries { get; }

    event Action? DragEnded;
    event Action? DragStarted;
    event Action? EntriesChanged;
    event Action<Guid>? FunctionBlockCreationRequested;

    void CreateLibraryEntries(IEnumerable<FunctionBlockDesign> functionBlockDesigns);
    void InvokeDragEnded();
    void InvokeDragStarted();
    void RequestFunctionBlockCreation(Guid designId);
}
