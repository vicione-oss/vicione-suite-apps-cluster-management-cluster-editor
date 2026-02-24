using System.Collections.Generic;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Core.Contracts;
using ViciOne.Ui.ClusterEditor.Models;
using ViciOne.Ui.ClusterEditor.Models.DiagramModels;
using ViciOne.Ui.ClusterEditor.Services;

namespace ViciOne.Ui.ClusterEditor.Extensions;

internal static class FunctionBlockNodeExtensions
{
    private static FbSetting CreateFbSetting(Datastore datastore, FunctionBlock functionBlock, Setting setting)
    {
        var settingDesign = datastore.Builder.ResolveSettingDesign(setting);
        var settingType = DataTypeCompatibilityValidator.DetermineValueType(settingDesign.SettingType);
        return new FbSetting(settingDesign.DefaultValue, functionBlock, setting, settingType);
    }

    public static IEnumerable<FbSetting> GetSettings(this IEnumerable<FunctionBlockNode> functionBlockNodes, Datastore datastore)
        => datastore.DataflowDiagramMapping
            .GetModels(functionBlockNodes)
            .SelectMany(fb => fb.Settings.Select(setting => CreateFbSetting(datastore, fb, setting)));
}
