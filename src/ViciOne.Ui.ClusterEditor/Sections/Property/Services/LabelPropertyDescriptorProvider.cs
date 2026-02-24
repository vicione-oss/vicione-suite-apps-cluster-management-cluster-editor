using System.Collections.Generic;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Services;
using ViciOne.Ui.Localization.Resources;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

internal sealed class LabelPropertyDescriptorProvider<TContext>(Datastore datastore)
    : IPropertyDescriptorProvider<TContext, Label>
{
    public IEnumerable<IPropertyDescriptor<Label>> GetPropertyDescriptors(TContext context)
    {
        var editor = datastore.Builder.Editors.Label;

        yield return new PropertyDescriptor<Label, string?>
        {
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => LabelColors.BackgroundDefault,
            GetValue = (instance) => instance.BackColor,
            Name = nameof(Label.BackColor),
            ResetValue = (instance) => editor.SetBackColor(instance, LabelColors.BackgroundDefault),
            SetValue = editor.SetBackColor
        };

        yield return new PropertyDescriptor<Label, string?>
        {
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => LabelColors.BorderDefault,
            GetValue = (instance) => instance.BorderColor,
            Name = nameof(Label.BorderColor),
            ResetValue = (instance) => editor.SetBorderColor(instance, LabelColors.BorderDefault),
            SetValue = editor.SetBorderColor
        };

        var settings = datastore.Builder.Settings;

        yield return new NumericPropertyDescriptor<Label, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.X,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(Label.X),
            SetValue = (instance, value) => editor.SetLocation(instance, new(value, instance.Y))
        };

        yield return new NumericPropertyDescriptor<Label, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.Y,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(Label.Y),
            SetValue = (instance, value) => editor.SetLocation(instance, new(instance.X, value))
        };

        yield return new NumericPropertyDescriptor<Label, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.Width,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(Label.Width),
            SetValue = (instance, value) => editor.SetSize(instance, new(value, instance.Height))
        };

        yield return new NumericPropertyDescriptor<Label, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.Height,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(Label.Height),
            SetValue = (instance, value) => editor.SetSize(instance, new(instance.Width, value))
        };
    }
}
