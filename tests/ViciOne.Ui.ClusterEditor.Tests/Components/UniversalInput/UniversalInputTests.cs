using System;
using System.Globalization;
using System.Threading.Tasks;
using Bunit;
using DevExpress.Blazor;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ViciOne.Ui.Blazor.Components.CheckBox;
using ViciOne.Ui.Blazor.Components.ComboBox;
using ViciOne.Ui.Blazor.Components.SpinEdit;
using ViciOne.Ui.Blazor.Components.SpinEdit.Services.Behaviors;
using ViciOne.Ui.Blazor.Components.TextBox;
using ViciOne.Ui.ClusterEditor.Components.UniversalInput.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.Components.UniversalInput.Extensions;
using ViciOne.Ui.ClusterEditor.Tests.Extensions;
using Xunit;
using UniversalInputComponent = ViciOne.Ui.ClusterEditor.Components.UniversalInput.UniversalInput;

namespace ViciOne.Ui.ClusterEditor.Tests.Components.UniversalInput;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "<Pending>")]
public class UniversalInputTests
{
    protected static BunitContext CreateTestContext()
    {
        var testContext = new BunitContext();
        testContext.Services.AddUniversalInput();
        testContext.JSInterop.Mode = JSRuntimeMode.Loose;

        return testContext;
    }

    public class TextInput : UniversalInputTests
    {
        private const string InitalValue = "SomeText";

        [Fact]
        public async Task Component_renders_string_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(string));
            });

            // Assert
            var textBox = component.FindComponent<TextBox>();
            Assert.NotNull(textBox);
            Assert.Equal(InitalValue, textBox.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChange = "OtherText";
            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(string));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is string s && s == valueChange; });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(valueChange);

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class BooleanInput : UniversalInputTests
    {
        private const bool InitalValue = true;

        [Fact]
        public async Task Component_renders_bool_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(bool));
            });

            // Assert
            var childComponent = component.FindComponent<CheckBox<bool>>();
            Assert.Equal(InitalValue, childComponent.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(bool));
                p.Add(c => c.ValueChanged, (s) => { valueChangedCalled = s is bool b && !b; });
            });

            // Act
            var childComponent = component.FindComponent<CheckBox<bool>>();
            var input = childComponent.Find("input");
            await input.InputAsync(EventArgs.Empty);

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class EnumInput : UniversalInputTests
    {
        private const DayOfWeek InitalValue = DayOfWeek.Monday;

        [Fact]
        public async Task Component_renders_enum_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();
            testContext.SetupDevExpressBlazor();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(DayOfWeek));
            });

            Assert.NotNull(component);

            // Assert
            var childComponent = component.FindComponent<DxComboBox<ComboBoxItem<DayOfWeek, string>, DayOfWeek>>();
            Assert.Equal(InitalValue, childComponent.Instance.Value);
        }

        [Fact(Skip = "Reactivate after ComboBox is used again")]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(DayOfWeek));
                p.Add(c => c.ValueChanged, (v) =>
                {
                    valueChangedCalled = v is DayOfWeek day && day == DayOfWeek.Saturday;
                });
            });

            // Act
            component
                .FindComponent<ComboBox<ComboBoxItem<DayOfWeek, string>, DayOfWeek>>()
                .SelectItemAtIndex(6);

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class UriInput : UniversalInputTests
    {
        private readonly Uri _initalValue = new("https://some.address.com/");

        [Fact]
        public async Task Component_renders_uri_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, _initalValue);
                p.Add(c => c.ValueType, typeof(Uri));
            });

            // Assert
            var textBox = component.FindComponent<TextBox>();
            Assert.NotNull(textBox);
            Assert.Equal(_initalValue.ToString(), textBox.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChange = new Uri("https://other.address.web/");
            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, _initalValue);
                p.Add(c => c.ValueType, typeof(Uri));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is Uri u && u.ToString() == valueChange.ToString(); });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(valueChange.ToString());

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }

        [Fact]
        public async Task Component_triggers_value_changed_if_emptied()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, _initalValue);
                p.Add(c => c.ValueType, typeof(Uri));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is string s && string.IsNullOrEmpty(s); });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(string.Empty);

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class CharInput : UniversalInputTests
    {
        private const char InitalValue = 'A';

        [Fact]
        public async Task Component_renders_char_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(char));
            });

            // Assert
            var textBox = component.FindComponent<TextBox>();
            Assert.NotNull(textBox);
            Assert.Equal(InitalValue.ToString(), textBox.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(char));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is char c && c == 'B'; });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange("B");

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }

        [Fact]
        public async Task Component_triggers_value_changed_if_emptied()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(char));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is char c && c == default; });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(string.Empty);

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class DateTimeInput : UniversalInputTests
    {
        private static readonly DateTime s_initalValue = new(2024, 6, 15, 10, 30, 0);

        [Fact]
        public async Task Component_renders_datetime_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, s_initalValue);
                p.Add(c => c.ValueType, typeof(DateTime));
            });

            // Assert
            var textBox = component.FindComponent<TextBox>();
            Assert.NotNull(textBox);
            Assert.Equal(s_initalValue.ToString(CultureInfo.CurrentCulture), textBox.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChange = new DateTime(2025, 1, 1, 12, 0, 0);
            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, s_initalValue);
                p.Add(c => c.ValueType, typeof(DateTime));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is DateTime dt && dt == valueChange; });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(valueChange.ToString(CultureInfo.CurrentCulture));

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class TimeSpanInput
    {
        private static readonly TimeSpan s_initalValue = TimeSpan.FromHours(2.5);

        [Fact]
        public async Task Component_renders_timespan_value()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, s_initalValue);
                p.Add(c => c.ValueType, typeof(TimeSpan));
            });

            // Assert
            var textBox = component.FindComponent<TextBox>();
            Assert.NotNull(textBox);
            Assert.Equal(s_initalValue.ToString(), textBox.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateTestContext();

            var valueChange = TimeSpan.FromMinutes(45);
            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, s_initalValue);
                p.Add(c => c.ValueType, typeof(TimeSpan));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is TimeSpan ts && ts == valueChange; });
            });

            // Act
            var childComponent = component.FindComponent<TextBox>();
            childComponent.TextEditChange(valueChange.ToString());

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }
    }

    public class NumericInput : UniversalInputTests
    {
        private const int InitalValue = 42;

        [Fact]
        public async Task Component_renders_numeric_value()
        {
            // Arrange
            await using var testContext = CreateNumericTestContext();

            // Act
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(int));
            });

            // Assert
            var childComponent = component.FindComponent<SpinEdit<int, int, int>>();
            Assert.Equal(InitalValue, childComponent.Instance.Value);
        }

        [Fact]
        public async Task Component_triggers_value_changed()
        {
            // Arrange
            await using var testContext = CreateNumericTestContext();

            var valueChangedCalled = false;
            var component = testContext.Render<UniversalInputComponent>(p =>
            {
                p.Add(c => c.Value, InitalValue);
                p.Add(c => c.ValueType, typeof(int));
                p.Add(c => c.ValueChanged, (v) => { valueChangedCalled = v is int i && i == 100; });
            });

            // Act
            var childComponent = component.FindComponent<SpinEdit<int, int, int>>();
            await childComponent.InvokeAsync(() => childComponent.Instance.ValueChanged.InvokeAsync(100));

            // Assert
            component.WaitForAssertion(() =>
            {
                Assert.True(valueChangedCalled);
            });
        }

        private static BunitContext CreateNumericTestContext()
        {
            var testContext = CreateTestContext();
            testContext.Services.AddSingleton(Substitute.For<ISpinBehavior<int, int, int>>());

            return testContext;
        }
    }
}
