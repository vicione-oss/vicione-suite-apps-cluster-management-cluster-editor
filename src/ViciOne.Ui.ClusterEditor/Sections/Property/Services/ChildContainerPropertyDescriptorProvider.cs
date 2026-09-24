using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Models.Descriptors;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services;
using ViciOne.Ui.ClusterEditor.Constants;
using ViciOne.Ui.ClusterEditor.Helpers;
using ViciOne.Ui.ClusterEditor.Sections.Property.Validators;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.Localization.Resources;
using LocalCommonVocabulary = ViciOne.Ui.ClusterEditor.Localization.Resources.CommonVocabulary;

namespace ViciOne.Ui.ClusterEditor.Sections.Property.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated through dependency injection")]
internal sealed class ChildContainerPropertyDescriptorProvider<TContext>(IDatastore datastore)
    : IPropertyDescriptorProvider<TContext, ChildContainer>
{
    private readonly CssColorPropertyValueValidator _cssColorPropertyValueValidator = new();

    public IEnumerable<IPropertyDescriptor<ChildContainer>> GetPropertyDescriptors(TContext context)
    {
        var editor = datastore.Builder.Editors.Container;

        yield return new PropertyDescriptor<ChildContainer, string>
        {
            Category = LocalCommonVocabulary.Data,
            GetValue = (instance) => instance.Name,
            Name = nameof(ChildContainer.Name),
            SetValue = editor.SetName
        };

        yield return new PropertyDescriptor<ChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => null,
            GetValue = (instance) => instance.Description,
            Name = nameof(ChildContainer.Description),
            ResetValue = editor.ResetDescription,
            SetValue = editor.SetDescription
        };

        yield return new PropertyDescriptor<ChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.BackgroundDefault,
            GetValue = (instance) => instance.BackColor,
            Name = nameof(ChildContainer.BackColor),
            ResetValue = (instance) => editor.SetBackColor(instance, BlockNodeColors.BackgroundDefault),
            SetValue = (instance, value) =>
            {
                if (value is null || Color.IsValidCssColor(value))
                    editor.SetBackColor(instance, value);
            },
            ValueValidators = [_cssColorPropertyValueValidator]
        };

        yield return new PropertyDescriptor<ChildContainer, string?>
        {
            CanBeSetToNull = true,
            Category = CommonVocabulary.Appearance,
            GetDefaultValue = (instance) => BlockNodeColors.ForegroundDefault,
            GetValue = (instance) => instance.ForeColor,
            Name = nameof(ChildContainer.ForeColor),
            ResetValue = (instance) => editor.SetForeColor(instance, BlockNodeColors.ForegroundDefault),
            SetValue = (instance, value) =>
            {
                if (value is null || Color.IsValidCssColor(value))
                    editor.SetForeColor(instance, value);
            },
            ValueValidators = [_cssColorPropertyValueValidator]
        };

        var settings = datastore.Builder.Settings;

        yield return new NumericPropertyDescriptor<ChildContainer, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.X ?? 0,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(ChildContainer.X),
            SetValue = (instance, value) => editor.SetLocation(instance, new(value, instance.Y ?? 0))
        };

        yield return new NumericPropertyDescriptor<ChildContainer, int, int, int>
        {
            Category = TechnicalTerms.Layout,
            GetValue = (instance) => instance.Y ?? 0,
            Interval = settings.GridSize,
            IsRasteredValue = true,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
            Name = nameof(ChildContainer.Y),
            SetValue = (instance, value) => editor.SetLocation(instance, new(instance.X ?? 0, value))
        };
    }
}
