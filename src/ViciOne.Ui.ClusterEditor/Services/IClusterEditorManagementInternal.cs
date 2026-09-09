using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Ui.ClusterEditor.Services;

/// <summary>
/// Internal interface for DataManagementService to expose additional methods that are only used within the ClusterEditor project and should not be accessible to external consumers.
/// </summary>
internal interface IClusterEditorManagementInternal : IClusterEditorManagement
{
    Task RequestLoadFbDesigns();
    Task RequestSave();
    Task ShowMessageToast(LogLevel logLevel, string message, Action clickCallback);
}
