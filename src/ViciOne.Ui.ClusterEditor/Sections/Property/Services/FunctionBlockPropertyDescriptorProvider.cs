using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ViciOne.Cluster.Builder.Extensions;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.Localization.Resources;
using LocalCommonVocabulary = ViciOne.Ui.ClusterEditor.Localization.Resources.CommonVocabulary;
using LocalTechnicalTerms = ViciOne.Ui.ClusterEditor.Localization.Resources.TechnicalTerms;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

internal sealed class FunctionBlockPropertyDescriptorProvider<TContext>(IDatastore datastore)
    : IPropertyDescriptorProvider<TContext, FunctionBlock>
{
    public IEnumerable<IPropertyDescriptor<FunctionBlock>> GetPropertyDescriptors(TContext context)
    {
        var builder = datastore.Builder;
        var editor = builder.Editors.FunctionBlock;

        yield return new PropertyDescriptor<FunctionBlock, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.BackgroundDefault,
            GetValue = (instance) => instance.BackColor,
            Name = nameof(FunctionBlock.BackColor),
            ResetValue = (instance) => editor.SetBackColor(instance, BlockNodeColors.BackgroundDefault),
            SetValue = editor.SetBackColor
        };

        var runModePropertyDescriptor = new SelectionPropertyDescriptor<FunctionBlock, FunctionBlockRunMode>
        {
            Category = LocalTechnicalTerms.RunModeSettingPlural,
            GetDefaultValue = editor.GetDefaultRunMode,
            GetSelectableValues = (instance) => editor.GetValidRunModes(instance)
                .Select(v => new SelectableValue<FunctionBlockRunMode> { Text = v.ToString(), Value = v }),
            GetValue = (instance) => instance.RunMode,
            Name = nameof(FunctionBlock.RunMode),
            ResetValue = editor.ResetRunMode,
            SetValue = editor.SetRunMode
        };

        yield return runModePropertyDescriptor;

        yield return new NumericPropertyDescriptor<FunctionBlock, uint, uint, uint>
        {
            Category = LocalTechnicalTerms.RunModeSettingPlural,
            DependsOn = [runModePropertyDescriptor],
            GetDefaultValue = (instance) => builder.ResolveDefaultCycleFrequency(instance.DesignId),
            GetValue = (instance) => instance.CycleFrequency,
            Interval = 1,
            Maximum = uint.MaxValue,
            Minimum = uint.MinValue,
            Name = nameof(FunctionBlock.CycleFrequency),
            ResetValue = editor.ResetCycleFrequency,
            SetValue = editor.SetCycleFrequency,
            Visible = (instance) => instance.RunMode == FunctionBlockRunMode.Cyclic
        };

        yield return new PropertyDescriptor<FunctionBlock, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetValue = (instance) => instance.Description,
            Name = nameof(FunctionBlock.Description),
            ResetValue = editor.ResetDescription,
            SetValue = editor.SetDescription
        };

        yield return new PropertyDescriptor<FunctionBlock, string?>
        {
            Category = LocalCommonVocabulary.Data,
            Enabled = (instance) => false,
            GetValue = (instance) => instance.Engine?.Name ?? string.Empty,
            Name = nameof(FunctionBlock.Engine)
        };

        yield return new PropertyDescriptor<FunctionBlock, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.ForegroundDefault,
            GetValue = (instance) => instance.ForeColor,
            Name = nameof(FunctionBlock.ForeColor),
            ResetValue = (instance) => editor.SetForeColor(instance, BlockNodeColors.ForegroundDefault),
            SetValue = editor.SetForeColor
        };

        var settings = builder.Settings;

        yield return new NumericPropertyDescriptor<FunctionBlock, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.X ?? 0,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(FunctionBlock.X),
            SetValue = (instance, value) => editor.SetLocation(instance, new Point(value, instance.Y ?? 0))
        };

        yield return new NumericPropertyDescriptor<FunctionBlock, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.Y ?? 0,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(FunctionBlock.Y),
            SetValue = (instance, value) => editor.SetLocation(instance, new Point(instance.X ?? 0, value))
        };

        yield return new PropertyDescriptor<FunctionBlock, string>
        {
            Category = LocalCommonVocabulary.Data,
            GetDefaultValue = (instance) => builder.ResolveFunctionBlockDesign(instance.DesignId).Name,
            GetValue = (instance) => instance.Name,
            Name = nameof(FunctionBlock.Name),
            SetValue = editor.SetName
        };
    }
}
