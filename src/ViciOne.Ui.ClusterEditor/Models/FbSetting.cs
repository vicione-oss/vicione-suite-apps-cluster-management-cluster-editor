using System;
using ViciOne.Cluster.Model;

namespace ViciOne.Ui.ClusterEditor.Models;

public record FbSetting
{
    private readonly FunctionBlock _functionBlock;
    private readonly object? _originalValue;

    public string FbName
        => _functionBlock.Name;

    public bool IsModified => !Equals(_originalValue, Value);

    public DateTime LastModified
        => Setting.ValueChangedTime.ToLocalTime();

    public string Name
        => Setting.Name;

    public Setting Setting { get; }
    public Type SettingType { get; }
    public object? Value { get; set; }

    public FbSetting(
        object? defaultValue,
        FunctionBlock functionBlock,
        Setting setting,
        Type settingType)
    {
        _functionBlock = functionBlock;

        Setting = setting;
        SettingType = settingType;
        Value = _originalValue = setting.Value ?? defaultValue;
    }
}
