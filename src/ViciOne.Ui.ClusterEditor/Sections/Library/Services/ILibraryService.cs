using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    event Func<Guid, Task>? FunctionBlockCreationRequested;

    void CreateLibraryEntries(IEnumerable<FunctionBlockDesign> functionBlockDesigns);
    void InvokeDragEnded();
    void InvokeDragStarted();
    Task RequestFunctionBlockCreation(Guid designId);
}
