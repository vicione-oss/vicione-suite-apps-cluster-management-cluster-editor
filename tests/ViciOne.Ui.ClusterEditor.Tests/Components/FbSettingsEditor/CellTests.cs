using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using ViciOne.Cluster.Model;
using ViciOne.Ui.Blazor.Components.CheckBox;
using ViciOne.Ui.Blazor.Components.PropertyGrid.Services.TypeDescriptors;
using ViciOne.Ui.Blazor.Components.SpinEdit.Extensions;
using ViciOne.Ui.ClusterEditor.Components.FbSettingsEditor;
using ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;
using ViciOne.Ui.ClusterEditor.Services.ClusterServices;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.FbSettingsEditor;

public class CellTests
{
    private const string CellModulePath =
        "./_content/ViciOne.Ui.ClusterEditor/Components/FbSettingsEditor/Cell.razor.js";

    [Fact]
    public async Task Non_boolean_value_renders_as_read_text()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, "hello", typeof(string));

        // Assert
        component.Markup.Should().Contain("hello");
        component.FindAll(".input-container").Should().BeEmpty();
    }

    [Fact]
    public async Task Modified_value_renders_bold_modifier_class()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, "hello", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.Modified, true));

        // Assert
        component.FindAll(".cell-read.modified").Should().ContainSingle();
    }

    [Fact]
    public async Task Unmodified_value_renders_without_the_modifier_class()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, "hello", typeof(string));

        // Assert
        component.FindAll(".cell-read").Should().ContainSingle();
        component.FindAll(".cell-read.modified").Should().BeEmpty();
    }

    [Fact]
    public async Task Boolean_setting_renders_a_checkbox_instead_of_read_text()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, true, typeof(bool));

        // Assert
        component.FindAll(".cell-read").Should().BeEmpty();
        component.FindAll(".check-box input[type=checkbox]").Should().ContainSingle();
    }

    [Fact]
    public async Task Nullable_boolean_without_a_value_renders_an_indeterminate_checkbox()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, null, typeof(bool?));

        // Assert
        var checkBox = component.FindComponent<CheckBox<bool?>>();
        checkBox.Instance.AllowIndeterminateState.Should().BeTrue();
        component.FindAll(".check-box.indeterminate").Should().ContainSingle();
    }

    [Fact]
    public async Task Toggling_the_checkbox_commits_the_new_value()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        object? committedValue = null;
        var commitCount = 0;

        var component = RenderCell(ctx, true, typeof(bool), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value =>
            {
                committedValue = value;
                commitCount++;
            }));
        var checkBox = component.FindComponent<CheckBox<bool?>>();

        // Act
        await component.InvokeAsync(() => checkBox.Instance.ValueChanged.InvokeAsync(false));

        // Assert
        commitCount.Should().Be(1);
        committedValue.Should().Be(false);
    }

    [Fact]
    public async Task Focusing_read_cell_enters_edit_mode()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        var component = RenderCell(ctx, "hello", typeof(string));

        // Act
        EnterEditMode(component);

        // Assert
        component.FindAll(".input-container").Should().ContainSingle();
    }

    // The keyboard route must not drift from the pointer one: this is what focusing the cell already does.
    [Fact]
    public async Task Activated_cell_enters_edit_mode_and_takes_the_focus()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        var module = SetupCellModule(ctx);

        // Act
        var component = RenderCell(ctx, "hello", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.Activated, true));

        // Assert
        component.FindAll(".input-container").Should().ContainSingle();
        component.WaitForAssertion(() => module.VerifyInvoke("focusFirstInput"));
    }

    // A boolean cell renders its checkbox whether or not it is editing, so Space toggles the value instead
    // of selecting the row.
    [Fact]
    public async Task Activated_boolean_cell_hands_the_focus_to_its_checkbox()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        var module = SetupCellModule(ctx);

        // Act
        var component = RenderCell(ctx, true, typeof(bool), additionalParameters: parameters => parameters
            .Add(p => p.Activated, true));

        // Assert
        component.FindAll(".input-container").Should().BeEmpty();
        component.FindAll(".check-box input[type=checkbox]").Should().ContainSingle();
        component.WaitForAssertion(() => module.VerifyInvoke("focusFirstInput"));
    }

    [Fact]
    public async Task Dropping_the_activation_leaves_the_edit_open()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);

        var component = RenderCell(ctx, "hello", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.Activated, true));

        // Act
        component.Render(parameters => parameters
            .Add(p => p.Activated, false));

        // Assert
        component.FindAll(".input-container").Should().ContainSingle();
    }

    [Fact]
    public async Task Repeated_activation_keeps_what_has_been_typed()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        object? committedValue = null;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.Activated, true)
            .Add(p => p.OnValueCommitted, value => committedValue = value));
        Type(component, "after");

        // Act
        component.Render(parameters => parameters
            .Add(p => p.Activated, true));
        // Blurring is the only way into the commit path, which reports what the editor still holds.
        Blur(component);

        // Assert
        component.WaitForAssertion(() => committedValue.Should().Be("after"));
    }

    [Fact]
    public async Task Cell_is_a_tab_stop()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, "hello", typeof(string));

        // Assert
        component.Find(".cell").GetAttribute("tabindex").Should().Be("0");
    }

    // The checkbox is a tab stop of its own, so a focusable container would stop the keyboard twice on one cell.
    [Fact]
    public async Task Boolean_cell_leaves_the_tab_stop_to_its_checkbox()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        // Act
        var component = RenderCell(ctx, true, typeof(bool));

        // Assert
        component.Find(".cell").GetAttribute("tabindex").Should().Be("-1");
    }

    // Destroying a focused element blurs it, and the focus-out that follows looks exactly like the user
    // leaving the cell.
    [Fact]
    public async Task The_focused_element_survives_entering_edit_mode()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        var component = RenderCell(ctx, "hello", typeof(string));

        // Act
        EnterEditMode(component);

        // Assert
        component.FindAll(".cell-read").Should().BeEmpty();
        component.FindAll(".cell").Should().ContainSingle();
    }

    // The container precedes the editor in the document, so a Shift+Tab out of the editor would otherwise land
    // back on the cell it is leaving.
    [Fact]
    public async Task Editing_cell_hands_its_tab_stop_to_the_editor()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);

        var component = RenderCell(ctx, "hello", typeof(string));

        // Act
        EnterEditMode(component);

        // Assert
        component.Find(".cell").GetAttribute("tabindex").Should().Be("-1");
    }

    [Fact]
    public async Task Losing_focus_commits_the_edited_value()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        object? committedValue = null;
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value =>
            {
                committedValue = value;
                commitCount++;
            }));
        EnterEditModeAndType(component, "after");

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() =>
        {
            commitCount.Should().Be(1);
            committedValue.Should().Be("after");
        });
    }

    [Fact]
    public async Task Losing_focus_without_changing_the_value_does_not_commit()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditMode(component);

        // Act
        Blur(component);

        // Assert
        // Leaving edit mode is what proves the focus-out path ran, so a missing commit is a real result
        // rather than the assertion arriving too early.
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    // This is the data-loss case: the shared "All" cell of a row whose function blocks hold different values
    // renders empty, so committing that empty value would overwrite every value in the row.
    [Fact]
    public async Task Losing_focus_on_an_empty_cell_without_typing_does_not_commit()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, null, typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditMode(component);

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    [Fact]
    public async Task Restoring_the_original_value_before_losing_focus_does_not_commit()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditModeAndType(component, "after");
        Type(component, "before");

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    // The editor keeps the last accepted value, so a rejected value entered on top of an accepted one would
    // commit the accepted one on the way out if the validation message were not checked.
    [Fact]
    public async Task A_rejected_value_entered_after_an_accepted_one_is_not_committed()
    {
        // Arrange
        const string Message = "Value is not allowed for this setting.";
        const string RejectedValue = "rejected";

        await using var datastore = CreateDatastore();
        datastore.Builder.Editors.Setting
            .When(editor => editor.ValidateValue(Arg.Any<Setting>(), Arg.Is<object?>(value =>
                Equals(value, RejectedValue))))
            .Do(_ => throw new InvalidOperationException(Message));

        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditModeAndType(component, "accepted");
        Type(component, RejectedValue);

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    [Fact]
    public async Task The_edited_value_is_validated_against_the_supplied_setting()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var setting = Substitute.For<Setting>();

        var component = RenderCell(ctx, "before", typeof(string), setting);

        // Act
        EnterEditModeAndType(component, "after");

        // Assert
        datastore.Builder.Editors.Setting.Received(1).ValidateValue(setting, "after");
    }

    [Fact]
    public async Task Enter_commits_and_parks_focus_on_the_table()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        var module = SetupCellModule(ctx);

        var component = RenderCell(ctx, "before", typeof(string));
        EnterEditModeAndType(component, "after");

        // Act
        component.Find(".cell").KeyDown(Key.Enter);

        // Assert
        component.WaitForAssertion(() => module.VerifyInvoke("focusTable"));
    }

    // A boolean cell renders no editor, so a key gated on the edit flag being visible would stop dead on it and
    // leave the traversal broken in the middle of a column.
    [Fact]
    public async Task Enter_parks_focus_from_a_boolean_cell_too()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        var module = SetupCellModule(ctx);

        var component = RenderCell(ctx, true, typeof(bool));

        // Act
        component.Find(".cell").KeyDown(Key.Enter);

        // Assert
        component.WaitForAssertion(() => module.VerifyInvoke("focusTable"));
    }

    // The arrow key bubbles from the SpinEdit to the cell. Rendering the cell for it hands the SpinEdit its
    // parameters again between key-down and key-up, which reset the step before it was reported.
    [Fact]
    public async Task Arrow_up_in_a_numeric_editor_steps_the_value()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        ctx.Services.AddIntSpinEdit();

        // The library registers its numeric descriptors only internally; without one the editor gets an interval of 0.
        var intDescriptor = Substitute.For<INumericValueTypeDescriptor<int>>();
        intDescriptor.One.Returns(1);
        intDescriptor.Minimum.Returns(int.MinValue);
        intDescriptor.Maximum.Returns(int.MaxValue);
        ctx.Services.AddSingleton(intDescriptor);

        SetupCellModule(ctx);
        object? committedValue = null;

        var component = RenderCell(ctx, 1, typeof(int), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value => committedValue = value));
        EnterEditMode(component);

        var input = component.Find(".spin-edit input");
        input.TriggerEvent("onfocusin", new FocusEventArgs());

        // Act
        input.KeyDown(new KeyboardEventArgs { Code = "ArrowUp", Key = "ArrowUp" });
        component.Find(".spin-edit input").KeyUp(new KeyboardEventArgs { Code = "ArrowUp", Key = "ArrowUp" });
        // Blurring is the only way into the commit path, which reports the stepped value.
        Blur(component);

        // Assert
        component.WaitForAssertion(() => committedValue.Should().Be(2));
    }

    [Fact]
    public async Task Escape_discards_the_edit_and_leaves_edit_mode()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        var module = SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditModeAndType(component, "after");

        // Act
        component.Find(".cell").KeyDown(Key.Escape);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        component.Markup.Should().Contain("before");
        commitCount.Should().Be(0);
        module.VerifyInvoke("focusTable");
    }

    // Handing focus back to the table blurs the editor, so the focus-out arrives after Escape has already left
    // edit mode. That order is what keeps the discarded value from being committed on the way out.
    [Fact]
    public async Task Focus_out_following_an_escape_commits_nothing()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditModeAndType(component, "after");
        component.Find(".cell").KeyDown(Key.Escape);

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    [Fact]
    public async Task Escape_discards_a_rejected_value_along_with_its_message()
    {
        // Arrange
        const string Message = "Value is not allowed for this setting.";
        const string RejectedValue = "rejected";

        await using var datastore = CreateDatastore();
        datastore.Builder.Editors.Setting
            .When(editor => editor.ValidateValue(Arg.Any<Setting>(), Arg.Is<object?>(value =>
                Equals(value, RejectedValue))))
            .Do(_ => throw new InvalidOperationException(Message));

        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);

        var component = RenderCell(ctx, "before", typeof(string));
        EnterEditModeAndType(component, RejectedValue);

        // Act
        component.Find(".cell").KeyDown(Key.Escape);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        component.Markup.Should().NotContain(Message);
    }

    // Committing the "All" cell fans its value out to the whole row while the next cell's editor is already open,
    // because the commit is what the focus change triggers. An editor left showing the old value writes it back
    // over the fanned-out one on the way out.
    [Fact]
    public async Task An_untouched_open_editor_adopts_a_value_committed_elsewhere()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);

        var component = RenderCell(ctx, "before", typeof(string));
        EnterEditMode(component);

        // Act
        component.Render(parameters => parameters.Add(p => p.Value, "fanned out"));

        // Assert
        component.Find("input").GetAttribute("value").Should().Be("fanned out");
    }

    [Fact]
    public async Task An_untouched_open_editor_does_not_write_back_over_a_value_committed_elsewhere()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, _ => commitCount++));
        EnterEditMode(component);
        component.Render(parameters => parameters.Add(p => p.Value, "fanned out"));

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        commitCount.Should().Be(0);
    }

    [Fact]
    public async Task An_open_editor_keeps_what_was_typed_when_the_value_changes_elsewhere()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);

        var component = RenderCell(ctx, "before", typeof(string));
        EnterEditModeAndType(component, "typed");

        // Act
        component.Render(parameters => parameters.Add(p => p.Value, "fanned out"));

        // Assert
        component.Find("input").GetAttribute("value").Should().Be("typed");
    }

    [Fact]
    public async Task An_open_editor_commits_what_was_typed_when_the_value_changed_elsewhere()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        object? committedValue = null;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value => committedValue = value));
        EnterEditModeAndType(component, "typed");
        component.Render(parameters => parameters.Add(p => p.Value, "fanned out"));

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() => committedValue.Should().Be("typed"));
    }

    [Fact]
    public async Task An_edit_after_an_escaped_one_still_commits()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        object? committedValue = null;
        var commitCount = 0;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value =>
            {
                committedValue = value;
                commitCount++;
            }));
        EnterEditModeAndType(component, "discarded");
        component.Find(".cell").KeyDown(Key.Escape);
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        EnterEditModeAndType(component, "kept");

        // Act
        Blur(component);

        // Assert
        component.WaitForAssertion(() =>
        {
            commitCount.Should().Be(1);
            committedValue.Should().Be("kept");
        });
    }

    [Fact]
    public async Task Value_the_datastore_rejects_shows_its_message()
    {
        // Arrange
        const string Message = "Value is not allowed for this setting.";

        await using var datastore = CreateDatastore();
        datastore.Builder.Editors.Setting
            .When(editor => editor.ValidateValue(Arg.Any<Setting>(), Arg.Any<object?>()))
            .Do(_ => throw new InvalidOperationException(Message));

        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);

        var component = RenderCell(ctx, "before", typeof(string));

        // Act
        EnterEditModeAndType(component, "rejected");

        // Assert
        component.Markup.Should().Contain(Message);
    }

    [Fact]
    public async Task Value_the_datastore_rejects_is_not_committed()
    {
        // Arrange
        await using var datastore = CreateDatastore();
        datastore.Builder.Editors.Setting
            .When(editor => editor.ValidateValue(Arg.Any<Setting>(), Arg.Any<object?>()))
            .Do(_ => throw new InvalidOperationException("Value is not allowed for this setting."));

        await using var ctx = CreateContext(datastore);
        SetupCellModule(ctx);
        object? committedValue = null;

        var component = RenderCell(ctx, "before", typeof(string), additionalParameters: parameters => parameters
            .Add(p => p.OnValueCommitted, value => committedValue = value));
        EnterEditModeAndType(component, "rejected");

        // Act
        // Blurring is the only way into the commit path, so a rejection is only proven by leaving the cell.
        Blur(component);

        // Assert
        component.WaitForAssertion(() => component.FindAll(".input-container").Should().BeEmpty());
        committedValue.Should().NotBe("rejected");
    }

    [Fact]
    public async Task Validation_message_clears_once_an_accepted_value_is_entered()
    {
        // Arrange
        const string Message = "Value is not allowed for this setting.";
        const string RejectedValue = "rejected";

        await using var datastore = CreateDatastore();
        datastore.Builder.Editors.Setting
            .When(editor => editor.ValidateValue(Arg.Any<Setting>(), Arg.Is<object?>(value =>
                Equals(value, RejectedValue))))
            .Do(_ => throw new InvalidOperationException(Message));

        await using var ctx = CreateContext(datastore);

        var component = RenderCell(ctx, "before", typeof(string));
        EnterEditModeAndType(component, RejectedValue);

        // Act
        Type(component, "accepted");

        // Assert
        component.Markup.Should().NotContain(Message);
    }

    // A bare substitute accepts every value; the real datastore has no cluster loaded here, so reading
    // its builder would throw and every edit would look invalid.
    private static IDatastore CreateDatastore()
        => Substitute.For<IDatastore>();

    // A substituted datastore is registered before SetupDatastore, whose registrations are all TryAdd.
    private static BunitContext CreateContext(IDatastore datastore)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        ctx.Services.AddScoped(_ => datastore);
        ctx.SetupDatastore();
        ctx.Services.AddUniversalInput();

        return ctx;
    }

    private static BunitJSModuleInterop SetupCellModule(BunitContext ctx)
    {
        var module = ctx.JSInterop.SetupModule(CellModulePath);
        module.Setup<bool>("isFocusInside", _ => true).SetResult(false);
        module.SetupVoid("focusFirstInput", _ => true);
        module.SetupVoid("focusTable", _ => true);

        return module;
    }

    // The editor imports the module once and hands it to its cells. bUnit completes the import synchronously,
    // both for a module set up by SetupCellModule and in loose mode.
    private static IJSObjectReference ImportCellModule(BunitContext ctx)
        => ctx.JSInterop.JSRuntime
            .InvokeAsync<IJSObjectReference>("import", CellModulePath)
            .AsTask()
            .GetAwaiter()
            .GetResult();

    private static IRenderedComponent<Cell> RenderCell(BunitContext ctx, object? value, Type valueType,
        Setting? setting = null, Action<ComponentParameterCollectionBuilder<Cell>>? additionalParameters = null)
        => ctx.Render<Cell>(parameters =>
        {
            parameters
                .Add(p => p.JsModule, ImportCellModule(ctx))
                .Add(p => p.Setting, setting ?? Substitute.For<Setting>())
                .Add(p => p.Value, value)
                .Add(p => p.ValueType, valueType);

            additionalParameters?.Invoke(parameters);
        });

    private static void Blur(IRenderedComponent<Cell> component)
        => component.Find(".cell").TriggerEvent("onfocusout", new FocusEventArgs());

    // bUnit does not simulate the browser focusing the nearest focusable ancestor of what is clicked, so the
    // click path into an edit cannot be reached from here.
    private static void EnterEditMode(IRenderedComponent<Cell> component)
        => component.Find(".cell").TriggerEvent("onfocusin", new FocusEventArgs());

    private static void EnterEditModeAndType(IRenderedComponent<Cell> component, string text)
    {
        EnterEditMode(component);
        Type(component, text);
    }

    // The editor reports its value on Enter or on blur, never on a keystroke.
    private static void Type(IRenderedComponent<Cell> component, string text)
    {
        var input = component.Find("input");
        input.Input(text);
        input.KeyUp(Key.Enter);
    }
}
