using Microsoft.AspNetCore.Components;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Sections.File.Components;

public sealed partial class FileSection : ComponentBase
{
    [Inject]
    private IClusterEditorManagementInternal DataManagementService { get; set; } = default!;
}
