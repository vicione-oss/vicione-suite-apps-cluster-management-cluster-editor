#if DEBUG
namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Models;

public sealed record DebugLogMessage
{
    public required string CallerFilePath { get; init; }
    public required int CallerLineNumber { get; init; }
    public required string CallerMemberName { get; init; }
    public required string Message { get; init; }
}
#endif
