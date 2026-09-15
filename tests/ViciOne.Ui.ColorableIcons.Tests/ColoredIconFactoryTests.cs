using System.Diagnostics.CodeAnalysis;
using ViciOne.Cluster.Model;
using Xunit;

namespace ViciOne.Ui.ColorableIcons.Tests;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Internal classes cannot be run as tests in VS")]
public static class ColoredIconFactoryTests
{
    public sealed class GetConnectorIcon
    {
        [Fact]
        public void Returns_correct_icon_input()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<g fill=\"none\" fill-rule=\"evenodd\">" +
                        "<path fill=\"#F0F\" fill-opacity=\".25\" d=\"M22 8v16h-8v-2h-2V10h2V8h8Z\"/>" +
                        "<path fill=\"#F0F\" d=\"M24 6v20H12v-4h2v2h8V8h-8v2h-2V6h12Z\"/>" +
                        "<path fill=\"#FFF\" d=\"m16 16-5.16 5-1.54-1.5 2.52-2.45H4v-2.11h7.82L9.3 12.5l1.54-1.5L16 16Z\"/>" +
                    "</g>" +
                "</svg>";

            var result = ColoredIconFactory.GetConnectorIcon("#F0F", true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_output()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<g fill=\"none\" fill-rule=\"evenodd\">" +
                        "<path fill=\"#F0F\" d=\"M12 6v20h12v-4h-2v2h-8V8h8v2h2V6H12Z\"/>" +
                        "<path fill=\"#F0F\" fill-opacity=\".25\" d=\"M22 24h-8V8h8v2h2v12h-2v2Z\"/>" +
                        "<path fill=\"#FFF\" d=\"m32 16-5.16 5-1.54-1.5 2.52-2.45H20v-2.11h7.82L25.3 12.5l1.54-1.5L32 16Z\"/>" +
                    "</g>" +
                "</svg>";

            var result = ColoredIconFactory.GetConnectorIcon("#F0F", false);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_correct_color_hex()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<g fill=\"none\" fill-rule=\"evenodd\">" +
                        "<path fill=\"#6A5\" fill-opacity=\".25\" d=\"M22 8v16h-8v-2h-2V10h2V8h8Z\"/>" +
                        "<path fill=\"#6A5\" d=\"M24 6v20H12v-4h2v2h8V8h-8v2h-2V6h12Z\"/>" +
                        "<path fill=\"#FFF\" d=\"m16 16-5.16 5-1.54-1.5 2.52-2.45H4v-2.11h7.82L9.3 12.5l1.54-1.5L16 16Z\"/>" +
                    "</g>" +
                "</svg>";

            var result = ColoredIconFactory.GetConnectorIcon("#6A5", true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_correct_color_rgb()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<g fill=\"none\" fill-rule=\"evenodd\">" +
                        "<path fill=\"rgb(50,65,98)\" fill-opacity=\".25\" d=\"M22 8v16h-8v-2h-2V10h2V8h8Z\"/>" +
                        "<path fill=\"rgb(50,65,98)\" d=\"M24 6v20H12v-4h2v2h8V8h-8v2h-2V6h12Z\"/>" +
                        "<path fill=\"#FFF\" d=\"m16 16-5.16 5-1.54-1.5 2.52-2.45H4v-2.11h7.82L9.3 12.5l1.54-1.5L16 16Z\"/>" +
                    "</g>" +
                "</svg>";

            var result = ColoredIconFactory.GetConnectorIcon("rgb(50,65,98)", true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_correct_size()
        {
            var expected =
                "<svg height=\"16px\" width=\"16px\" viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<g fill=\"none\" fill-rule=\"evenodd\">" +
                        "<path fill=\"#F0F\" fill-opacity=\".25\" d=\"M22 8v16h-8v-2h-2V10h2V8h8Z\"/>" +
                        "<path fill=\"#F0F\" d=\"M24 6v20H12v-4h2v2h8V8h-8v2h-2V6h12Z\"/>" +
                        "<path fill=\"#FFF\" d=\"m16 16-5.16 5-1.54-1.5 2.52-2.45H4v-2.11h7.82L9.3 12.5l1.54-1.5L16 16Z\"/>" +
                    "</g>" +
                "</svg>";

            var result = ColoredIconFactory.GetConnectorIcon("#F0F", true, 16);

            Assert.Equal(expected, result);
        }
    }

    public sealed class GetDataPortIcon()
    {
        [Fact]
        public void Returns_correct_icon_direction_in_connected_to_input()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<rect fill=\"#F0F\" fill-rule=\"evenodd\" height=\"11\" id=\"filled\"  rx=\".96\" width=\"11\" x=\"10.5\" y=\"10.5\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.In, false, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_in_not_connected()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M20.51 10.5c.55 0 .99.44.99.99v9.02c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02Zm-.39 1.38h-8.25v8.24h8.26v-8.25Z\" id=\"empty\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.In);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_in_out_connected_to_input()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M20.51 10.5c.55 0 .99.44.99.99v9.02c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02Zm-.39 1.38H16v8.24h4.13v-8.25Z\" id=\"input-filled\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.InOut, false, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_in_out_connected_to_input_and_output()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<rect fill=\"#F0F\" fill-rule=\"evenodd\" height=\"11\" id=\"filled\"  rx=\".96\" width=\"11\" x=\"10.5\" y=\"10.5\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.InOut, true, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_in_out_connected_to_output()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M21.5 20.51c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02c.55 0 .99.44.99.99v9.02ZM16 11.87h-4.13v8.26H16v-8.25Z\" id=\"output-filled\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.InOut, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_in_out_not_connected()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M20.51 10.5c.55 0 .99.44.99.99v9.02c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02Zm-.39 1.38h-8.25v8.24h8.26v-8.25Z\" id=\"empty\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.InOut);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_out_connected_to_output()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<rect fill=\"#F0F\" fill-rule=\"evenodd\" height=\"11\" id=\"filled\"  rx=\".96\" width=\"11\" x=\"10.5\" y=\"10.5\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.Out, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_correct_icon_direction_out_not_connected()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M20.51 10.5c.55 0 .99.44.99.99v9.02c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02Zm-.39 1.38h-8.25v8.24h8.26v-8.25Z\" id=\"empty\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.Out);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_correct_color_rgb()
        {
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<rect fill=\"rgb(50,65,98)\" fill-rule=\"evenodd\" height=\"11\" id=\"filled\"  rx=\".96\" width=\"11\" x=\"10.5\" y=\"10.5\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("rgb(50,65,98)", DataPortDirection.InOut, true, true);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_correct_size()
        {
            var expected =
                "<svg height=\"16px\" width=\"16px\" viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<rect fill=\"#F0F\" fill-rule=\"evenodd\" height=\"11\" id=\"filled\"  rx=\".96\" width=\"11\" x=\"10.5\" y=\"10.5\" />" +
                    "<path d=\"M10.5 7.06 7.06 10.5 6.03 9.47l1.74-1.74H2.5V6.27h5.27L6.03 4.53 7.06 3.5l3.44 3.44-.06.06.06.06Z\" id=\"arrow-in\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                    "<path d=\"m28.5 7.06-3.44 3.44-1.03-1.03 1.74-1.74H20.5V6.27h5.27l-1.74-1.74 1.03-1.03 3.44 3.44-.06.06.06.06Z\" id=\"arrow-out\" fill=\"#FFF\" fill-rule=\"evenodd\" />" +
                "</svg>";

            var result = ColoredIconFactory.GetDataPortIcon("#F0F", DataPortDirection.InOut, true, true, 16);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_icon_with_no_arrows_when_direction_is_null()
        {
            // Arrange: a node with no effective direction at all (e.g. narrowed below its parent's
            // directions until nothing overlaps) draws the plain body without any arrow.
            var expected =
                "<svg viewBox=\"0 0 32 32\" xmlns=\"http://www.w3.org/2000/svg\">" +
                    "<path d=\"M20.51 10.5c.55 0 .99.44.99.99v9.02c0 .55-.44.99-.99.99h-9.02a.99.99 0 0 1-.99-.99v-9.02c0-.55.44-.99.99-.99h9.02Zm-.39 1.38h-8.25v8.24h8.26v-8.25Z\" id=\"empty\" fill=\"#F0F\" fill-rule=\"evenodd\" />" +
                "</svg>";

            // Act
            var result = ColoredIconFactory.GetDataPortIcon("#F0F", null);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Returns_filled_icon_without_arrows_when_direction_is_null_and_something_is_linked()
        {
            // Act: losing the direction must not hide a link that is still there.
            var result = ColoredIconFactory.GetDataPortIcon("#F0F", null, true, true);

            // Assert
            Assert.Contains("id=\"filled\"", result, StringComparison.InvariantCulture);
            Assert.DoesNotContain("id=\"arrow-in\"", result, StringComparison.InvariantCulture);
            Assert.DoesNotContain("id=\"arrow-out\"", result, StringComparison.InvariantCulture);
        }
    }
}
