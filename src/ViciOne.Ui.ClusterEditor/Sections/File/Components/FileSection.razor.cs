using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.File.Components;

public sealed partial class FileSection : ComponentBase
{
#if DEBUG
    private const string DebugCssClass = "debug";
#else
    private const string DebugCssClass = "";
#endif

    [Inject]
    private IClusterEditorManagementInternal DataManagementService { get; set; } = default!;
}
