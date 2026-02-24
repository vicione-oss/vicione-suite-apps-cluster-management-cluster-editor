#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ViciOne.Ui.ClusterEditor.Sections.Debugging.Models;

namespace ViciOne.Ui.ClusterEditor.Sections.Debugging.Services;

public sealed class DebugService
{
    private const int LogHistorySize = 100;

    private readonly Dictionary<string, Action> _debugActions = [];
    private readonly List<DebugLogMessage> _debugLog = [];
    private readonly Dictionary<string, List<string>> _debugValues = [];
    private int _valueHistorySize = 10;

    public bool ShowMessageLog { get; set; }
    public int VisualLogHeightPx { get; set; } = 250;

    public event Action? ActionChanged;
    public event Action? LogChanged;
    public event Action? ValueChanged;

    public void AddValue(string name, string value)
    {
        if (!_debugValues.ContainsKey(name))
            _debugValues.Add(name, []);

        _debugValues[name].Add(value);

        if (_debugValues[name].Count > _valueHistorySize)
            _debugValues[name].RemoveAt(0);

        ValueChanged?.Invoke();
    }

    public IDictionary<string, Action> GetActions()
        => _debugActions;

    public Dictionary<string, string> GetKeyValuePairs()
    {
        var result = new Dictionary<string, string>();

        foreach (var kv in _debugValues)
            result.Add(kv.Key, GetValue(kv.Key));

        return result;
    }

    public IEnumerable<DebugLogMessage> GetLog()
        => _debugLog;

    public string GetValue(string name)
        => _debugValues[name].LastOrDefault() ?? string.Empty;

    public IEnumerable<string> GetValueHistory(string name)
        => _debugValues[name];

    public void Log(
        string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerPath = "",
        [CallerLineNumber] int callerLine = 0)
    {
        _debugLog.Add(new()
        {
            CallerFilePath = callerPath,
            CallerLineNumber = callerLine,
            CallerMemberName = callerName,
            Message = message,
        });

        if (_debugLog.Count > LogHistorySize)
            _debugLog.RemoveAt(0);

        LogChanged?.Invoke();
    }

    public void SetAction(string name, Action action)
    {
        if (!_debugActions.TryAdd(name, action))
            _debugActions[name] = action;

        ActionChanged?.Invoke();
    }

    public void SetValueHistorySize(int size)
    {
        _valueHistorySize = size;
        foreach (var kv in _debugValues)
        {
            if (kv.Value.Count > _valueHistorySize)
                kv.Value.RemoveRange(0, kv.Value.Count - _valueHistorySize);
        }
    }
}
#endif
